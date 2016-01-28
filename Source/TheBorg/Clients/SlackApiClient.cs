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
using Newtonsoft.Json;
using Serilog;
using TheBorg.Clients.Slack.ApiResponses;
using TheBorg.Clients.Slack.DTOs;
using TheBorg.ValueObjects;

namespace TheBorg.Clients
{
    public class SlackApiClient : ISlackApiClient
    {
        private readonly ILogger _logger;
        private readonly IRestClient _restClient;
        private readonly ConcurrentDictionary<string, Task<User>> _userCache = new ConcurrentDictionary<string, Task<User>>();
        private static readonly Tenant Tenant = new Tenant("slack");

        public SlackApiClient(
            ILogger logger,
            IRestClient restClient)
        {
            _logger = logger;
            _restClient = restClient;
        }

        public Task<User> GetUserAsync(string userId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(userId)) return Task.FromResult(null as User);

            return _userCache.GetOrAdd(
                userId,
                id => InternalGetUserAsync(id, cancellationToken));
        }

        private async Task<User> InternalGetUserAsync(string userId, CancellationToken cancellationToken)
        {
            var userInfoApiResponse = await CallApiAsync<UserInfoApiResponse>(
                "users.info",
                cancellationToken,
                new KeyValuePair<string, string>("user", userId))
                .ConfigureAwait(false);
            return new User(userInfoApiResponse.User.Name, userInfoApiResponse.User.Id, Tenant);
        }

        public Task<ApiResponse> SendMessageAsync(
            string channelId,
            string text,
            CancellationToken cancellationToken)
        {
            return CallApiAsync<ApiResponse>(
                "chat.postMessage",
                new Dictionary<string, string>
                    {
                        {"channel", channelId},
                        {"text", text},
                        {"as_user", "true"},
                    },
                cancellationToken);
        }

        public async Task<T> CallApiAsync<T>(
            string method,
            Dictionary<string, string> arguments,
            CancellationToken cancellationToken)
        {
            if (!arguments.ContainsKey("token"))
            {
                arguments.Add("token", Environment.GetEnvironmentVariable("SLACK_TOKEN"));
            }
            var json = await _restClient.GetAsync(
                new Uri(new Uri("https://slack.com/api/"), method),
                arguments,
                cancellationToken)
                .ConfigureAwait(false);
            return JsonConvert.DeserializeObject<T>(json);
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