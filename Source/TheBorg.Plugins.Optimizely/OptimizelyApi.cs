// The MIT License(MIT)
// 
// Copyright(c) 2016 Christian Ølholm
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
using TheBorg.Plugins.Optimizely.DTO;

namespace TheBorg.Plugins.Optimizely
{
    public class OptimizelyApi : IPluginHttpApi
    {
        private readonly Uri _baseUri = new Uri("https://www.optimizelyapis.com");
        private readonly IConfigApi _configApi;
        private readonly IHttpApi _httpApi;
        private readonly IMessageApi _messageApi;
        
        public OptimizelyApi(
            IConfigApi configApi,
            IHttpApi httpApi,
            IMessageApi messageApi)
        {
            _configApi = configApi;
            _httpApi = httpApi;
            _messageApi = messageApi;
        }

        private async Task<List<Project>> GetProjectsAsync(CancellationToken cancellationToken)
        {
            var apiToken = await _configApi.GetAsync(ConfigKey.With("api-token"), cancellationToken).ConfigureAwait(false);
            var apiTokenHeader = new KeyValuePair<string, string>("token", apiToken);
            var headers = new List<KeyValuePair<string, string>>() { apiTokenHeader };
            var uri = new Uri(_baseUri, "experiment/v1/projects/");

            return await _httpApi.GetAsyncAs<List<Project>>(uri, cancellationToken, headers).ConfigureAwait(false);
        }

        [HttpApi(HttpApiMethod.Get, "api/commands/projects")]
        [Command("^opt list projects", "Lists projects")]
        public async Task ListProjects([FromBody]TenantMessage tenantMessage, CancellationToken cancellationToken)
        {
            var projects = await GetProjectsAsync(cancellationToken).ConfigureAwait(false);
            var result = projects.Aggregate(
                new StringBuilder().AppendLine("*Projects:*"),
                (b, p) => b.AppendLine($"*{p.project_name}* - {p.id} - [{p.project_status}]"))
                .ToString();

            await _messageApi.SendAsync(tenantMessage.CreateReply(result), cancellationToken).ConfigureAwait(false);
        }
    }
}
