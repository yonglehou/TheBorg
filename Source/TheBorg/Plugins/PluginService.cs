// The MIT License(MIT)
// 
// Copyright(c) 2016 Rasmus Mikkelsen
// https://github.com/rasmus/TheBorg
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
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Halibut.ServiceModel;
using Serilog;
using TheBorg.Core;
using TheBorg.Host;

namespace TheBorg.Plugins
{
    public class PluginService : IPluginService
    {
        private readonly ILogger _logger;
        private readonly AppDomainManager _appDomainManager = new AppDomainManager();
        private readonly X509Certificate2 _serverCertifiate = CertificateGenerator.CreateSelfSignCertificate();
        private readonly DelegateServiceFactory _delegateServiceFactory = new DelegateServiceFactory();

        public PluginService(
            ILogger logger)
        {
            _logger = logger;

            _delegateServiceFactory.Register();
        }

        public Task<IPluginProxy> LoadPluginAsync(string dllPath)
        {
            if (!File.Exists(dllPath)) throw new ArgumentException($"Plugin '{dllPath}' does not exist");

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
            var bytes = CertificateGenerator.CreateSelfSignCertificatePfx(DateTime.MinValue, DateTime.MaxValue);
            var clientThumbprint = new X509Certificate2(bytes).Thumbprint;

            var pluginHost = appDomain.CreateInstanceAndUnwrap(Assembly.GetAssembly(typeof(PluginHost)).FullName, typeof(PluginHost).ToString()) as PluginHost;
            ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        pluginHost.Launch(dllPath, appDomain, bytes, _serverCertifiate.Thumbprint, 0);
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, $"Plugin '{friendlyName}' failed");
                        AppDomain.Unload(appDomain);
                    }
                });

            return Task.FromResult<IPluginProxy>(new PluginProxy(appDomain));
        }
    }
}