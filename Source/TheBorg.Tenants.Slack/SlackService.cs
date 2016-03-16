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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using TheBorg.Common.Clients;
using TheBorg.Common.Serialization;
using TheBorg.Interface.ValueObjects.Tenants;
using TheBorg.Tenants.Slack.ApiResponses;

namespace TheBorg.Tenants.Slack
{
    public class SlackService : ISlackService
    {
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IRestClient _restClient;
        private readonly ConcurrentDictionary<string, Task<TenantUser>> _userCache = new ConcurrentDictionary<string, Task<TenantUser>>();

        private static readonly TenantKey TenantKey = new TenantKey("slack");
        private static readonly ISet<string> ValidAttachmentProperties = new HashSet<string>
            {
                "fallback",
                "color",
                "pretext",
                "author_name",
                "author_link",
                "author_icon",
                "title",
                "title_link",
                "text",
                //"fields" // TODO: Don't know how to handle yet
                "image_url",
                "thumb_url"
            }; 

        public SlackService(
            ILogger logger,
            IJsonSerializer jsonSerializer,
            IRestClient restClient)
        {
            _logger = logger;
            _jsonSerializer = jsonSerializer;
            _restClient = restClient;
        }

        public Task<TenantUser> GetUserAsync(string userId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(userId)) return Task.FromResult(null as TenantUser);

            return _userCache.GetOrAdd(
                userId,
                id => InternalGetUserAsync(id, cancellationToken));
        }

        private async Task<TenantUser> InternalGetUserAsync(string userId, CancellationToken cancellationToken)
        {
            var userInfoApiResponse = await CallApiAsync<UserInfoApiResponse>(
                "users.info",
                cancellationToken,
                new KeyValuePair<string, string>("user", userId))
                .ConfigureAwait(false);
            return new TenantUser(userInfoApiResponse.User.Name, userInfoApiResponse.User.Id, TenantKey);
        }

        public async Task SendMessageAsync(TenantMessage tenantMessage, CancellationToken cancellationToken)
        {
            var attachments = tenantMessage.Attachments
                .Select(a => a.Properties
                    .Where(p =>
                        {
                            var isValid = ValidAttachmentProperties.Contains(p.Type);
                            if (!isValid)
                            {
                                _logger.Warning($"Invalid Slack attachment property '{p.Type}'");
                            }
                            return isValid;
                        })
                    .ToDictionary(kv => kv.Type, kv => kv.Data))
                .Where(a => a.Any())
                .ToList();

            var apiResponse = await CallApiAsync<ApiResponse>(
                "chat.postMessage",
                new Dictionary<string, string>
                    {
                        {"channel", tenantMessage.Address.TenantChannel.Value},
                        {"text", tenantMessage.Text},
                        {"as_user", "true"},
                        {"attachments", _jsonSerializer.Serialize(attachments, JsonFormat.LowerSnakeCase)}
                    },
                cancellationToken)
                .ConfigureAwait(false);

            if (!apiResponse.IsOk)
            {
                _logger.Error($"SLACK SEND MESSAGE: {apiResponse.Error}");
            }
        }

        public Task<T> CallApiAsync<T>(
            string method,
            Dictionary<string, string> arguments,
            CancellationToken cancellationToken)
        {
            if (!arguments.ContainsKey("token"))
            {
                arguments.Add("token", Environment.GetEnvironmentVariable("SLACK_TOKEN"));
            }

            return _restClient.PostFormAsync<T>(
                new Uri(new Uri("https://slack.com/api/"), method),
                arguments,
                JsonFormat.LowerSnakeCase,
                cancellationToken);
        }

        public Task<T> CallApiAsync<T>(
            string method,
            CancellationToken cancellationToken,
            params KeyValuePair<string, string>[] keyValuePairs)
        {
            var arguments = keyValuePairs
                .ToDictionary(
                    keyValuePair => keyValuePair.Key,
                    keyValuePair => keyValuePair.Value);
            return CallApiAsync<T>(method, arguments, cancellationToken);
        }
    }
}