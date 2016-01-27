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
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Serilog;
using TheBorg.Clients;
using TheBorg.Core;
using TheBorg.MessageClients.Slack;
using TheBorg.MessageClients.Slack.ApiResponses;
using TheBorg.MessageClients.Slack.RtmResponses;

namespace TheBorg.MessageClients
{
    public class SlackMessageClient : ISlackMessageClient
    {
        private readonly ILogger _logger;
        private readonly ISlackApiClient _slackApiClient;
        private readonly IWebSocketClient _webSocketClient;
        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private readonly Subject<SlackMessage> _messages = new Subject<SlackMessage>();
        private int _messageIdCounter;
        private readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new UnderscoreMappingResolver(),
            };

        public IObservable<SlackMessage> Messages => _messages; 

        public SlackMessageClient(
            ILogger logger,
            ISlackApiClient slackApiClient,
            IWebSocketClient webSocketClient)
        {
            _logger = logger;
            _slackApiClient = slackApiClient;
            _webSocketClient = webSocketClient;

            _disposables.Add(_webSocketClient.Messages.Subscribe(Received));
        }

        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            var rtmStartResponse = await _slackApiClient.CallApiAsync<RtmStartApiResponse>(
                "rtm.start",
                cancellationToken)
                .ConfigureAwait(false);

            await _webSocketClient.ConnectAsync(rtmStartResponse.Url, cancellationToken).ConfigureAwait(false);
        }

        private int GetMessageId()
        {
            return Interlocked.Increment(ref _messageIdCounter);
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

            var username = _slackApiClient.GetUserAsync(messageRtmResponse.User, CancellationToken.None).Result?.Name ?? "<unknown>";
            _messages.OnNext(new SlackMessage(
                messageRtmResponse.Text,
                username,
                messageRtmResponse.Channel));
        }
    }
}