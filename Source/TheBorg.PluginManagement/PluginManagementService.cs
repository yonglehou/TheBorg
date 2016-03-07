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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using TheBorg.Core;
using TheBorg.Core.Clients;
using TheBorg.Core.Extensions;
using TheBorg.Core.Serialization;
using TheBorg.Interface.ValueObjects;
using TheBorg.Interface.ValueObjects.Plugins;
using TheBorg.PluginManagement.HttpApi;

namespace TheBorg.PluginManagement
{
    public class PluginManagementService : IPluginManagementService
    {
        private readonly ILogger _logger;
        private readonly IRestClient _restClient;
        private readonly IPluginInstaller _pluginInstaller;
        private readonly IPluginLoader _pluginLoader;
        private readonly IPluginHttpApi _pluginHttpApi;
        private readonly ConcurrentDictionary<PluginId, IPluginProxy> _plugins = new ConcurrentDictionary<PluginId, IPluginProxy>();
        private readonly ConcurrentDictionary<PluginId, IReadOnlyCollection<CommandDescription>> _pluginCommandDescriptions = new ConcurrentDictionary<PluginId, IReadOnlyCollection<CommandDescription>>();
        private readonly int _serverPort;
        private readonly Uri _pluginApiUri;

        public PluginManagementService(
            ILogger logger,
            IRestClient restClient,
            IPluginInstaller pluginInstaller,
            IPluginLoader pluginLoader,
            IPluginHttpApi pluginHttpApi)
        {
            _logger = logger;
            _restClient = restClient;
            _pluginInstaller = pluginInstaller;
            _pluginLoader = pluginLoader;
            _pluginHttpApi = pluginHttpApi;

            _serverPort = TcpHelper.GetFreePort();
            _pluginApiUri = new Uri($"http://127.0.0.1:{_serverPort}/");
        }

        public Task InitializeAsync(CancellationToken cancellationToken)
        {
            return _pluginHttpApi.StartAsync(_serverPort, cancellationToken);
        }

        public async Task<PluginInformation> LoadPluginAsync(PluginPath pluginPath, CancellationToken cancellationToken)
        {
            var plugin = await _pluginLoader.LoadPluginAsync(pluginPath, _pluginApiUri, cancellationToken).ConfigureAwait(false);
            var pluginInformation = await plugin.Plugin.GetPluginInformationAsync(cancellationToken).ConfigureAwait(false);

            _plugins.TryAdd(plugin.Id, plugin);

            return pluginInformation;
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

        public async Task<IReadOnlyCollection<PluginInformation>> LoadInstalledPluginsAsync(CancellationToken cancellationToken)
        {
            var pluginPaths = await _pluginInstaller.GetInstalledPluginsAsync(cancellationToken).ConfigureAwait(false);
            return await Task.WhenAll(pluginPaths.Select(p => LoadPluginAsync(p, cancellationToken))).ConfigureAwait(false);
        }

        public Task RegisterAsync(PluginId pluginId, IEnumerable<CommandDescription> commandDescriptions)
        {
            var commandDescriptionList = commandDescriptions.ToList();
            _pluginCommandDescriptions[pluginId] = commandDescriptionList;

            _logger.Verbose($"Registerd commands for '{pluginId}': {string.Join(", ", commandDescriptionList.Select(d => d.ToString()))}");

            return Task.FromResult(0);
        }

        public async Task<IReadOnlyCollection<PluginInformation>> GetPluginsAsync(CancellationToken cancellationToken)
        {
            return await Task.WhenAll(_plugins.Values.Select(p => p.Plugin.GetPluginInformationAsync(cancellationToken))).ConfigureAwait(false);
        }

        public async Task<PluginInformation> InstallPluginAsync(Uri uri, CancellationToken cancellationToken)
        {
            var filename = uri.GetFilename();
            if (!uri.ToString().ToLowerInvariant().EndsWith(".zip")) throw new ArgumentException($"Only understands ZIP files: {uri} ({filename})");

            using (var tempFile = await _restClient.DownloadAsync(uri, cancellationToken).ConfigureAwait(false))
            {
                var pluginPath = await _pluginInstaller.InstallPluginAsync(
                    Path.GetFileNameWithoutExtension(filename), // TODO: Fix
                    tempFile.Path,
                    PluginPackageType.Zip,
                    cancellationToken)
                    .ConfigureAwait(false);

                return await LoadPluginAsync(
                    pluginPath,
                    cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        public async Task<ProcessMessageResult> ProcessAsync(TenantMessage tenantMessage, CancellationToken cancellationToken)
        {
            foreach (var pluginCommandDescription in _pluginCommandDescriptions)
            {
                foreach (var commandDescription in pluginCommandDescription.Value)
                {
                    if (!commandDescription.IsMatch(tenantMessage.Text))
                    {
                        continue;
                    }

                    var plugin = _plugins[pluginCommandDescription.Key];
                    await _restClient.PostAsync<object, TenantMessage>(new Uri(plugin.Plugin.BaseUri, commandDescription.Endpoint),
                        tenantMessage,
                        JsonFormat.PascalCase,
                        cancellationToken)
                        .ConfigureAwait(false);
                    return ProcessMessageResult.Handled;
                }
            }

            return ProcessMessageResult.Skipped;
        }
    }
}