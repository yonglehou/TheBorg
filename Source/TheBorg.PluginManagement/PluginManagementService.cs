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
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TheBorg.Core;
using TheBorg.Core.Extensions;
using TheBorg.Interface.ValueObjects;
using TheBorg.PluginManagement.HttpApi;
using TheBorg.PluginManagement.ValueObjects;

namespace TheBorg.PluginManagement
{
    public class PluginManagementService : IPluginManagementService
    {
        private readonly IPluginLoader _pluginLoader;
        private readonly IPluginHttpApi _pluginHttpApi;
        private readonly ConcurrentDictionary<PluginId, IPluginProxy> _plugins = new ConcurrentDictionary<PluginId, IPluginProxy>();
        private readonly int _serverPort;
        private readonly Uri _pluginApiUri;

        public PluginManagementService(
            IPluginLoader pluginLoader,
            IPluginHttpApi pluginHttpApi)
        {
            _pluginLoader = pluginLoader;
            _pluginHttpApi = pluginHttpApi;

            _serverPort = TcpHelper.GetFreePort();
            _pluginApiUri = new Uri($"http://127.0.0.1:{_serverPort}/");
        }

        public Task InitializeAsync(CancellationToken cancellationToken)
        {
            return _pluginHttpApi.StartAsync(_serverPort, cancellationToken);
        }

        public async Task LoadPluginAsync(PluginPath pluginPath, CancellationToken cancellationToken)
        {
            var plugin = await _pluginLoader.LoadPluginAsync(pluginPath, _pluginApiUri, cancellationToken).ConfigureAwait(false);

            _plugins.TryAdd(plugin.Id, plugin);
        }

        public Task UnloadPluginAsync(PluginId pluginId)
        {
            if (!_plugins.ContainsKey(pluginId))
            {
                return Task.FromResult(0);
            }

            IPluginProxy pluginProxy;
            _plugins.TryRemove(pluginId, out pluginProxy);
            pluginProxy.Dispose();

            return Task.FromResult(0);
        }
    }
}