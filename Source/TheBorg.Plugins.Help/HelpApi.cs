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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TheBorg.Interface;
using TheBorg.Interface.Apis;
using TheBorg.Interface.Attributes;
using TheBorg.Interface.Extensions;
using TheBorg.Interface.ValueObjects;
using TheBorg.Interface.ValueObjects.Plugins;
using TheBorg.Interface.ValueObjects.Tenants;

namespace TheBorg.Plugins.Help
{
    public class HelpApi : IPluginHttpApi
    {
        private readonly IHttpApi _httpApi;
        private readonly IMessageApi _messageApi;
        private readonly IPluginApi _pluginApi;

        public HelpApi(
            IHttpApi httpApi,
            IMessageApi messageApi,
            IPluginApi pluginApi)
        {
            _httpApi = httpApi;
            _messageApi = messageApi;
            _pluginApi = pluginApi;
        }

        [HttpApi(HttpApiMethod.Post, "api/commands/help")]
        [Command("^help$", "list commands")]
        public async Task Help([FromBody] TenantMessage tenantMessage, CancellationToken cancellationToken)
        {
            var pluginInformations = await _pluginApi.GetPluginsAsync(cancellationToken).ConfigureAwait(false);

            var commandDescriptions = (await Task.WhenAll(pluginInformations.Select(i => GetCommandDescriptionAsync(i, cancellationToken))).ConfigureAwait(false))
                .SelectMany(l => l)
                .OrderBy(c => c.Command)
                .ToList();

            var commandHelp = commandDescriptions.Aggregate(new StringBuilder(), (b, c) => b.AppendLine(c.ToString())).ToString();

            await _messageApi.ReplyToAsync(tenantMessage, commandHelp, cancellationToken).ConfigureAwait(false);
        }

        private static readonly Regex HelpPlugin = new Regex(@"^help (?<pluginId>[a-z0-9\.]{3,})$", RegexOptions.Compiled);
        [HttpApi(HttpApiMethod.Post, "api/commands/help-plugin-information")]
        [Command(@"^help (?<pluginId>[a-z0-9\.]{3,})$", "list commands")]
        public async Task PluginInfo([FromBody] TenantMessage tenantMessage, CancellationToken cancellationToken)
        {
            var pId = HelpPlugin.Match(tenantMessage.Text).Groups["pluginId"].Value.ToLowerInvariant().Trim();
            var pluginInformations = await _pluginApi.GetPluginsAsync(cancellationToken).ConfigureAwait(false);
            var pluginInformation = pluginInformations.SingleOrDefault(i => i.Id.Value == pId);
            if (pluginInformation == null)
            {
                await _messageApi.ReplyToAsync(tenantMessage, $"There's no plugin with ID '{pId}'", cancellationToken).ConfigureAwait(false);
                return;
            }

            var text = new StringBuilder()
                .AppendLine($"ID: {pluginInformation.Id.Value} v{pluginInformation.Version.Value}")
                .AppendLine($"Title: {pluginInformation.Title.Value}")
                .AppendLine($"Description: {pluginInformation.Description.Value}")
                .ToString();

            await _messageApi.ReplyToAsync(tenantMessage, text, cancellationToken).ConfigureAwait(false);
        }

        private async Task<IReadOnlyCollection<HelpDetails>> GetCommandDescriptionAsync(
            PluginInformation pluginInformation,
            CancellationToken cancellationToken)
        {
            var commandDescriptions = await _httpApi.GetAsyncAs<IReadOnlyCollection<CommandDescription>>(
                new Uri(pluginInformation.Uri, "_plugin/commands"),
                cancellationToken)
                .ConfigureAwait(false);
            return commandDescriptions
                .Select(d => new HelpDetails(d.Regex, d.Help))
                .ToList();
        }

        private class HelpDetails
        {
            public readonly string Command;
            private readonly string _help;

            public HelpDetails(
                string command,
                string help)
            {
                _help = help;

                Command = command.PrettyRegex();
            }

            public override string ToString()
            {
                return $"{Command}: {_help}";
            }
        }
    }
}