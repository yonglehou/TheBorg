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
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Serilog;
using TheBorg.Clients.Slack;
using TheBorg.Clients.Slack.ApiResponses;
using TheBorg.Clients.Slack.DTOs;
using TheBorg.Clients.Slack.RtmMessages;
using TheBorg.Core;

namespace TheBorg.Clients
{
    public class SlackClient : ISlackClient
    {
        private readonly ILogger _logger;
        private readonly IRestClient _restClient;
        private readonly IWebSocketClient _webSocketClient;
        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private readonly Subject<SlackMessage> _messages = new Subject<SlackMessage>();
        private int _messageIdCounter;
        private readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new UnderscoreMappingResolver(),
            };
        private readonly ConcurrentDictionary<string, Task<UserDto>> _userCache = new ConcurrentDictionary<string, Task<UserDto>>(); 

        public IObservable<SlackMessage> Messages => _messages; 

        public SlackClient(
            ILogger logger,
            IRestClient restClient,
            IWebSocketClient webSocketClient)
        {
            _logger = logger;
            _restClient = restClient;
            _webSocketClient = webSocketClient;

            _disposables.Add(_webSocketClient.Messages.Subscribe(Received));
        }

        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            var rtmStartResponse = await CallApiAsync<RtmStartApiResponse>(
                "rtm.start",
                cancellationToken)
                .ConfigureAwait(false);

            await _webSocketClient.ConnectAsync(rtmStartResponse.Url, cancellationToken).ConfigureAwait(false);
        }

        private async Task<T> CallApiAsync<T>(
            string method,
            Dictionary<string, string> arguments,
            CancellationToken cancellationToken)
        {
            var json = await _restClient.GetAsync(
                new Uri(new Uri("https://slack.com/api/"), method),
                arguments,
                cancellationToken)
                .ConfigureAwait(false);
            return JsonConvert.DeserializeObject<T>(json);
        }

        private Task<T> CallApiAsync<T>(
            string method,
            CancellationToken cancellationToken,
            params KeyValuePair<string, string>[] keyValuePairs)
        {
            var arguments = keyValuePairs.ToDictionary(keyValuePair => keyValuePair.Key, keyValuePair => keyValuePair.Value);
            if (!arguments.ContainsKey("token"))
            {
                arguments.Add("token", Environment.GetEnvironmentVariable("SLACK_TOKEN"));
            }
            return CallApiAsync<T>(method, arguments, cancellationToken);
        }

        private int GetMessageId()
        {
            return Interlocked.Increment(ref _messageIdCounter);
        }

        private Task<UserDto> GetUserAsync(string userId)
        {
            return _userCache.GetOrAdd(
                userId,
                id => CallApiAsync<UserDto>(
                    "users.info",
                    CancellationToken.None,
                    new KeyValuePair<string, string>("user", userId)));
        }

        private void Received(string json)
        {
            var rtmMessage = JsonConvert.DeserializeObject<RtmResponse>(json, _jsonSerializerSettings);

            switch (rtmMessage.Type)
            {
                case "hello":
                    _logger.Information("Connected to Slack RTM web socket");
                    break;
                case "message":
                    ReceivedMessage(JsonConvert.DeserializeObject<MessageRtmResponse>(json, _jsonSerializerSettings));
                    break;
                default:
                    _logger.Verbose($"Ignoring Slack message type '{rtmMessage.Type}'");
                    break;
            }
        }

        private void ReceivedMessage(MessageRtmResponse messageRtmResponse)
        {
            _logger.Debug($"Slack message - {messageRtmResponse.User}@{messageRtmResponse.Channel}: {messageRtmResponse.Text}");

            var username = GetUserAsync(messageRtmResponse.User).Result.Name;
            _messages.OnNext(new SlackMessage(
                messageRtmResponse.Text,
                username));
        }
    }
}