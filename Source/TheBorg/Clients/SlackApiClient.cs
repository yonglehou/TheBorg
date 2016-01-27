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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TheBorg.Clients.Slack.DTOs;
using TheBorg.MessageClients.Slack.ApiResponses;

namespace TheBorg.Clients
{
    public class SlackApiClient : ISlackApiClient
    {
        private readonly IRestClient _restClient;
        private readonly ConcurrentDictionary<string, Task<UserDto>> _userCache = new ConcurrentDictionary<string, Task<UserDto>>();

        public SlackApiClient(
            IRestClient restClient)
        {
            _restClient = restClient;
        }

        public Task<UserDto> GetUserAsync(string userId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(userId)) return Task.FromResult(null as UserDto);

            return _userCache.GetOrAdd(
                userId,
                id => CallApiAsync<UserDto>(
                    "users.info",
                    cancellationToken,
                    new KeyValuePair<string, string>("user", id)));
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
                        {"icon_url", "https://media.githubusercontent.com/media/rasmus/TheBorg/master/icon-512x512.jpg"},
                        {"username", "The Borg"}
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