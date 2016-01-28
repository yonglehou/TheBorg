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
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Serilog;
using TheBorg.Clients;
using TheBorg.Clients.Slack.DTOs;
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

        private UserDto _self;

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
                new Dictionary<string, string>
                    {
                        {"simple_latest", "true"},
                        {"no_unreads", "true" },
                    },
                cancellationToken)
                .ConfigureAwait(false);
            _self = rtmStartResponse.Self;
            await _webSocketClient.ConnectAsync(rtmStartResponse.Url, cancellationToken).ConfigureAwait(false);
        }

        private int GetMessageId()
        {
            return Interlocked.Increment(ref _messageIdCounter);
        }

        private async void Received(string json)
        {
            try
            {
                var rtmMessage = JsonConvert.DeserializeObject<RtmResponse>(json, _jsonSerializerSettings);

                switch (rtmMessage.Type)
                {
                    case "hello":
                        _logger.Information("Connected to Slack RTM web socket");
                        break;
                    case "message":
                        await ReceivedMessage(JsonConvert.DeserializeObject<MessageRtmResponse>(json, _jsonSerializerSettings)).ConfigureAwait(false);
                        break;
                    default:
                        _logger.Verbose($"Ignoring Slack message type '{rtmMessage.Type}'");
                        break;
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, $"Failed to process received message JSON: {json}");
            }
        }

        private async Task ReceivedMessage(MessageRtmResponse messageRtmResponse)
        {
            if (messageRtmResponse.User == _self.Id)
            {
                _logger.Verbose($"Skipping message from myself: {messageRtmResponse.Text}");
                return;
            }
            if (!string.IsNullOrEmpty(messageRtmResponse.Subtype))
            {
                _logger.Verbose($"Skipping message with subtype '{messageRtmResponse.Subtype}': {messageRtmResponse.Text}");
                return;
            }
            if (messageRtmResponse.IsEdit)
            {
                _logger.Verbose($"Skipping message edit: {messageRtmResponse.Text}");
                return;
            }

            _logger.Debug($"Slack message - {messageRtmResponse.User}@{messageRtmResponse.Channel}: {messageRtmResponse.Text}");

            var user = await _slackApiClient.GetUserAsync(messageRtmResponse.User, CancellationToken.None).ConfigureAwait(false);

            _messages.OnNext(new SlackMessage(
                messageRtmResponse.Text,
                user?.Name ?? "<unknown>",
                messageRtmResponse.Channel));
        }
    }
}