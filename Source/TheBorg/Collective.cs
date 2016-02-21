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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using TheBorg.Commands;
using TheBorg.Conversations;
using TheBorg.Core;
using TheBorg.Core.Extensions;
using TheBorg.Interface.ValueObjects;
using TheBorg.PluginManagement;
using TheBorg.PluginManagement.ValueObjects;
using TheBorg.Plugins.Jokes;
using TheBorg.Tenants;

namespace TheBorg
{
    public class Collective : ICollective
    {
        private readonly ILogger _logger;
        private readonly IPluginManagementService _pluginManagementService;
        private readonly ICommandManager _commandManager;
        private readonly IConversationManager _conversationManager;
        private readonly IReadOnlyCollection<ITenant> _tenants;

        private readonly IReadOnlyCollection<string> BuiltInPlugins = new []
            {
                typeof(JokesPluginBootstrapper).Assembly.GetName().Name,
            };

        public Collective(
            ILogger logger,
            IPluginManagementService pluginManagementService,
            ICommandManager commandManager,
            IConversationManager conversationManager,
            IEnumerable<ITenant> tenants)
        {
            _logger = logger;
            _pluginManagementService = pluginManagementService;
            _commandManager = commandManager;
            _conversationManager = conversationManager;
            _tenants = tenants.ToList();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _pluginManagementService.InitializeAsync(cancellationToken).ConfigureAwait(false);
            await LoadBuildInPluginsAsync(cancellationToken).ConfigureAwait(false);

            var disposables = await Task.WhenAll(_tenants.Select(async t =>
                {
                    await t.ConnectAsync(cancellationToken).ConfigureAwait(false);
                    return t.Messages.Subscribe(HandleMessage);
                })).ConfigureAwait(false);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }

        private async void HandleMessage(TenantMessage tenantMessage)
        {
            // Yes, its async void

            foreach (var messageProcessor in MessageProcessors())
            {
                var processMessageResult = await messageProcessor.ProcessAsync(tenantMessage, CancellationToken.None).ConfigureAwait(false);
                if (processMessageResult == ProcessMessageResult.Handled)
                {
                    break;
                }
            }
        }

        private async Task LoadBuildInPluginsAsync(CancellationToken cancellationToken)
        {
            var applicationRoot = Path.GetDirectoryName(typeof (Collective).Assembly.GetCodeBase());

            foreach (var builtInPlugin in BuiltInPlugins)
            {
                try
                {
                    var pluginPath = new PluginPath(Path.Combine(applicationRoot, $"{builtInPlugin}.dll"));
                    await _pluginManagementService.LoadPluginAsync(
                        pluginPath,
                        cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _logger.Error(e, $"Failed to load '{builtInPlugin}'");
                }
            }
        }

        private IEnumerable<IMessageProcessor> MessageProcessors()
        {
            yield return _conversationManager;
            yield return _commandManager;
        } 
    }
}