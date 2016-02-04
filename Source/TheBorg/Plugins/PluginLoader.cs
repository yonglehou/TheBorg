// The MIT License(MIT)
// 
// Copyright(c) 2016 Rasmus Mikkelsen
// https://github.com/theborgbot/TheBorg
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Halibut;
using Halibut.ServiceModel;
using Serilog;
using TheBorg.Core;
using TheBorg.Host;
using TheBorg.Interface;

namespace TheBorg.Plugins
{
    public class PluginLoader : IPluginLoader
    {
        private readonly ILogger _logger;
        private readonly IPluginHostTransport _pluginHostTransport;
        private readonly AppDomainManager _appDomainManager = new AppDomainManager();
        private readonly X509Certificate2 _serverCertifiate = CertificateGenerator.CreateSelfSignCertificate();
        private readonly DelegateServiceFactory _delegateServiceFactory = new DelegateServiceFactory();
        private readonly HalibutRuntime _halibutRuntimeServer;
        private readonly int _tcpPort = TcpHelper.GetFreePort();

        public PluginLoader(
            ILogger logger,
            IPluginHostTransport pluginHostTransport)
        {
            _logger = logger;
            _pluginHostTransport = pluginHostTransport;

            _delegateServiceFactory.Register(() => _pluginHostTransport);
            _halibutRuntimeServer = new HalibutRuntime(_delegateServiceFactory, _serverCertifiate);
            _halibutRuntimeServer.Listen(new IPEndPoint(IPAddress.Loopback, _tcpPort));
        }

        public Task<IPluginProxy> LoadPluginAsync(string dllPath)
        {
            if (!File.Exists(dllPath)) throw new ArgumentException($"Plugin '{dllPath}' does not exist");

            var stopWatch = Stopwatch.StartNew();
            var appDomainSetup = new AppDomainSetup
                {
                    ShadowCopyFiles = "true",
                    LoaderOptimization = LoaderOptimization.MultiDomain,
                };
            var configFilePath = $"{dllPath}.config";
            if (File.Exists(configFilePath))
            {
                appDomainSetup.ConfigurationFile = configFilePath;
            }

            var friendlyName = Path.GetFileName(dllPath);
            var appDomain = _appDomainManager.CreateDomain(friendlyName, null, appDomainSetup);
            var bytes = CertificateGenerator.CreateSelfSignCertificatePfx(DateTime.Now.AddYears(-1), DateTime.Now.AddYears(40));
            var clientThumbprint = new X509Certificate2(bytes).Thumbprint;
            _halibutRuntimeServer.Trust(clientThumbprint);

            var pluginHost = appDomain.CreateInstanceAndUnwrap(Assembly.GetAssembly(typeof(PluginHostClient)).FullName, typeof(PluginHostClient).ToString()) as PluginHostClient;
            var autoResetEvent = new AutoResetEvent(false);
            var clientPort = TcpHelper.GetFreePort();

            try
            {
                pluginHost.Launch(dllPath, appDomain, bytes, _serverCertifiate.Thumbprint, _tcpPort, clientPort);
                autoResetEvent.Set();
            }
            catch (Exception e)
            {
                _logger.Error(e, $"Plugin '{friendlyName}' failed");
                AppDomain.Unload(appDomain);
                throw;
            }

            var halibutRuntime = new HalibutRuntime(_serverCertifiate);
            var pluginTransport = halibutRuntime.CreateClient<IPluginTransport>($"https://127.0.0.1:{clientPort}", clientThumbprint);

            stopWatch.Stop();
            _logger.Debug($"Loaded plugin '{friendlyName}' in {stopWatch.Elapsed.TotalSeconds:0.00} seconds");

            var pluginProxy = new PluginProxy(pluginTransport, halibutRuntime, appDomain);
            pluginProxy.Plugin.PingAsync(CancellationToken.None);

            return Task.FromResult<IPluginProxy>(pluginProxy);
        }
    }
}