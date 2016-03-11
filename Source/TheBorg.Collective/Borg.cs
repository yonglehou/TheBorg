﻿// The MIT License(MIT)
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using TheBorg.Collective.MsSql;
using TheBorg.Collective.PluginManagement;
using TheBorg.Common;
using TheBorg.Common.Extensions;
using TheBorg.Common.Tenants;
using TheBorg.Interface.ValueObjects;
using TheBorg.Interface.ValueObjects.Plugins;

namespace TheBorg.Collective
{
    public class Borg : IBorg
    {
        private readonly ILogger _logger;
        private readonly IMsSqlUpgrader _msSqlUpgrader;
        private readonly IPluginManagementService _pluginManagementService;
        private readonly IReadOnlyCollection<ITenant> _tenants;


        public Borg(
            ILogger logger,
            IMsSqlUpgrader msSqlUpgrader,
            IPluginManagementService pluginManagementService,
            IEnumerable<ITenant> tenants)
        {
            _logger = logger;
            _msSqlUpgrader = msSqlUpgrader;
            _pluginManagementService = pluginManagementService;
            _tenants = tenants.ToList();
        }

        public async Task StartAsync(IEnumerable<Assembly> builtInPlugins, CancellationToken cancellationToken)
        {
            using (_logger.Time("Upgrade database"))
            {
                await _msSqlUpgrader.UpgradeAsync().ConfigureAwait(false);
            }

            using (_logger.Time("TheBorg start"))
            {
                await _pluginManagementService.InitializeAsync(cancellationToken).ConfigureAwait(false);

                await Task.WhenAll(
                    LoadBuildInPluginsAsync(builtInPlugins, cancellationToken),
                    _pluginManagementService.LoadInstalledPluginsAsync(cancellationToken))
                    .ConfigureAwait(false);

                var disposables = await Task.WhenAll(_tenants.Select(async t =>
                    {
                        await t.ConnectAsync(cancellationToken).ConfigureAwait(false);
                        return t.Messages.Subscribe(HandleMessage);
                    })).ConfigureAwait(false);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.WhenAll(_tenants.Select(t => t.DisconnectAsync(cancellationToken)));
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

        private Task LoadBuildInPluginsAsync(IEnumerable<Assembly> builtInPlugins, CancellationToken cancellationToken)
        {
            var applicationRoot = Path.GetDirectoryName(typeof (Borg).Assembly.GetCodeBase());

            return Task.WhenAll(builtInPlugins
                .Select(a => a.GetName().Name)
                .Select(a => PluginPath.With(applicationRoot, $"{a}.dll"))
                .Select(p => _pluginManagementService.LoadPluginAsync(p, cancellationToken)));
        }

        private IEnumerable<IMessageProcessor> MessageProcessors()
        {
            yield return _pluginManagementService;
        } 
    }
}