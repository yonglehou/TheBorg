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

using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TheBorg.Interface;
using TheBorg.Interface.Apis;
using TheBorg.Interface.Attributes;
using TheBorg.Interface.ValueObjects;
using TheBorg.Interface.ValueObjects.Plugins;

namespace TheBorg.Plugins.Administration
{
    public class PluginsApi : IPluginHttpApi
    {
        private readonly IPluginApi _pluginApi;
        private readonly IMessageApi _messageApi;

        public PluginsApi(
            IPluginApi pluginApi,
            IMessageApi messageApi)
        {
            _pluginApi = pluginApi;
            _messageApi = messageApi;
        }

        [HttpApi(HttpApiMethod.Post, "api/commands/list-plugins")]
        public async Task ListPlugins([FromBody] TenantMessage tenantMessage, CancellationToken cancellationToken)
        {
            var pluginInformations = await _pluginApi.GetPluginsAsync(cancellationToken).ConfigureAwait(false);
            var stringBuilder = pluginInformations.Aggregate(new StringBuilder(), (s, i) => s.AppendLine($"{i.Id} - {i.Description}"));
            await _messageApi.ReplyToAsync(tenantMessage, stringBuilder.ToString(), cancellationToken).ConfigureAwait(false);
        }

        private static readonly Regex Regex = new Regex(@"^unload plugin (?<pluginId>[a-z0-9\.]+)$", RegexOptions.Compiled);
        [HttpApi(HttpApiMethod.Post, "api/commands/unload-plugin")]
        public async Task UnloadPlugin([FromBody] TenantMessage tenantMessage, CancellationToken cancellationToken)
        {
            var pId = Regex.Match(tenantMessage.Text).Groups["pluginId"].Value;
            var pluginId = PluginId.With(pId);
            await _pluginApi.UnloadPluginAsync(pluginId, cancellationToken).ConfigureAwait(false);
            await _messageApi.ReplyToAsync(tenantMessage, $"Unloaded plugin {pluginId}", cancellationToken).ConfigureAwait(false);
        }
    }
}