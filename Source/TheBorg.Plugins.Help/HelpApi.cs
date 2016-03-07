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
using System.Threading;
using System.Threading.Tasks;
using TheBorg.Interface;
using TheBorg.Interface.Apis;
using TheBorg.Interface.Attributes;
using TheBorg.Interface.ValueObjects;
using TheBorg.Interface.ValueObjects.Plugins;

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
                .OrderBy(c => c.Regex)
                .ToList();

            var commandHelp = commandDescriptions.Aggregate(new StringBuilder(), (b, c) => b.AppendLine($"{CleanRegEx(c.Regex)}: {c.Help}")).ToString();

            await _messageApi.ReplyToAsync(tenantMessage, commandHelp, cancellationToken).ConfigureAwait(false);
        }

        private Task<IReadOnlyCollection<CommandDescription>> GetCommandDescriptionAsync(
            PluginInformation pluginInformation,
            CancellationToken cancellationToken)
        {
            return _httpApi.GetAsyncAs<IReadOnlyCollection<CommandDescription>>(
                new Uri(pluginInformation.Uri, "_plugin/commands"),
                cancellationToken);
        }

        private static string CleanRegEx(string regex)
        {
            return regex.Trim('^', '$');
        }
    }
}