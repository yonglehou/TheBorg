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
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using TheBorg.Core;
using TheBorg.Core.Clients;
using TheBorg.Core.Extensions;
using TheBorg.Host;
using TheBorg.Interface.ValueObjects.Plugins;

namespace TheBorg.PluginManagement
{
    public class PluginLoader : IPluginLoader
    {
        private readonly ILogger _logger;
        private readonly IRestClient _restClient;
        private readonly AppDomainManager _appDomainManager = new AppDomainManager();

        public PluginLoader(
            ILogger logger,
            IRestClient restClient)
        {
            _logger = logger;
            _restClient = restClient;
        }

        public Task<IPluginProxy> LoadPluginAsync(PluginPath pluginPath, Uri pluginApiUri, CancellationToken cancellationToken)
        {
            return Task.Run(() => InternalLoadPluginAsync(pluginPath, pluginApiUri, cancellationToken), cancellationToken);
        }

        private async Task<IPluginProxy> InternalLoadPluginAsync(PluginPath pluginPath, Uri pluginApiUri, CancellationToken cancellationToken)
        {
            var pluginId = pluginPath.GetPluginId();

            using (_logger.Time($"Loading plugin '{pluginId}'"))
            {
                var appDomainSetup = new AppDomainSetup
                {
                    ShadowCopyFiles = "true",
                    LoaderOptimization = LoaderOptimization.MultiDomain,
                };
                var configFilePath = $"{pluginPath.Value}.config";
                if (File.Exists(configFilePath))
                {
                    appDomainSetup.ConfigurationFile = configFilePath;
                }

                var appDomain = _appDomainManager.CreateDomain(pluginId.Value, null, appDomainSetup);

                var pluginHost = appDomain.CreateInstanceAndUnwrap(Assembly.GetAssembly(typeof(PluginHostClient)).FullName, typeof(PluginHostClient).ToString()) as PluginHostClient;
                var autoResetEvent = new AutoResetEvent(false);
                var clientPort = TcpHelper.GetFreePort();

                try
                {
                    pluginHost.Launch(pluginPath.Value, appDomain, pluginApiUri, clientPort);
                    autoResetEvent.Set();
                }
                catch (Exception e)
                {
                    _logger.Error(e, $"Plugin '{pluginId}' failed");
                    AppDomain.Unload(appDomain);
                    throw;
                }

                var pluginProxy = new PluginProxy(
                    pluginId,
                    appDomain,
                    new Plugin(_logger, new Uri($"http://127.0.0.1:{clientPort}"),
                    _restClient));

                await pluginProxy.Plugin.PingAsync(cancellationToken).ConfigureAwait(false);

                return pluginProxy;
            }
        }
    }
}