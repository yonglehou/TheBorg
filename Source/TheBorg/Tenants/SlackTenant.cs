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
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Serilog;
using TheBorg.Core;
using TheBorg.Interface.Core;
using TheBorg.Interface.Tenants;
using TheBorg.Interface.ValueObjects;
using TheBorg.Services;
using TheBorg.Services.Slack.ApiResponses;
using TheBorg.Services.Slack.DTOs;
using TheBorg.Tenants.Slack.RtmResponses;

namespace TheBorg.Tenants
{
    public class SlackTenant : ITenant
    {
        private readonly ILogger _logger;
        private readonly ISlackService _slackService;
        private readonly IWebSocketClient _webSocketClient;
        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private readonly Subject<TenantMessage> _messages = new Subject<TenantMessage>();
        private readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new UnderscoreMappingResolver(),
            };
        private static readonly Tenant Tenant = new Tenant("slack");
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private int _messageId;

        private UserDto _self;

        public IObservable<TenantMessage> Messages => _messages; 

        public SlackTenant(
            ILogger logger,
            ISlackService slackService,
            IWebSocketClient webSocketClient)
        {
            _logger = logger;
            _slackService = slackService;
            _webSocketClient = webSocketClient;

            _disposables.Add(_webSocketClient.Messages.Subscribe(Received));
        }

        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            var rtmStartResponse = await _slackService.CallApiAsync<RtmStartApiResponse>(
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

        public Task DisconnectAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }

        private async void Received(string json)
        {
            // Yes, it's async void

            try
            {
                var rtmMessage = JsonConvert.DeserializeObject<RtmResponse>(json, _jsonSerializerSettings);

                switch (rtmMessage.Type)
                {
                    case "hello":
                        _logger.Information("Connected to Slack RTM web socket");
                        var t = Task.Run(async () => await PingLoopAsync()); // Not awaited
                        break;
                    case "pong":
                        {
                            var pongRtmResponse = JsonConvert.DeserializeObject<PongRtmResponse>(json, _jsonSerializerSettings);
                            _logger.Verbose($"Received pong from the server. Latency is {(DateTimeOffset.Now - pongRtmResponse.Time).TotalSeconds:0.00} seconds");
                        }
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

            var toMe = $"{_self.Id}: ";
            var text = SanitiseText(messageRtmResponse.Text);
            if (!text.StartsWith(toMe) && messageRtmResponse.Channel[0] != 'D')
            {
                _logger.Verbose($"Skipping message as its not for me: {messageRtmResponse.Text}");
                return;
            }

            text = text.Replace(toMe, string.Empty).Trim();
            _logger.Debug($"Slack message - {messageRtmResponse.User}@{messageRtmResponse.Channel}: {messageRtmResponse.Text}");

            var user = await _slackService.GetUserAsync(messageRtmResponse.User, CancellationToken.None).ConfigureAwait(false);
            var sender = new Address(
                user,
                new TenantChannel(messageRtmResponse.Channel),
                Tenant);

            _messages.OnNext(new TenantMessage(
                text,
                sender,
                (t, c) => ReplyToAsync(messageRtmResponse, t, c)));
        }

        private Task ReplyToAsync(
            MessageRtmResponse messageRtmResponse,
            string text,
            CancellationToken cancellationToken)
        {
            if (messageRtmResponse.Channel[0] != 'D')
            {
                text = $"<@{messageRtmResponse.User}>: {text}";
            }
            return _slackService.SendMessageAsync(messageRtmResponse.Channel, text, cancellationToken);
        }

        private static readonly string[] InvalidStrings = {"<", ">", "@"};
        private static string SanitiseText(string text)
        {
            return InvalidStrings.Aggregate(text, ((s, t) => s.Replace(t, string.Empty)));
        }

        private async Task PingLoopAsync()
        {
            try
            {
                while (true)
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), _cancellationTokenSource.Token).ConfigureAwait(false);
                    var messageId = Interlocked.Increment(ref _messageId);
                    var json = JsonConvert.SerializeObject(
                        new
                        {
                            id = messageId,
                            type = "ping",
                            time = DateTimeOffset.Now,
                        },
                        _jsonSerializerSettings);
                    await _webSocketClient.SendAsync(json, _cancellationTokenSource.Token).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "Slack ping loop failed");
            }
        }
    }
}