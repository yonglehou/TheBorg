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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using TheBorg.Clients;
using TheBorg.Commands.Attributes;
using TheBorg.MessageClients.Slack;
using TheBorg.MessageClients.Slack.ApiResponses;

namespace TheBorg.Commands.CommandSets
{
    public class SlackCommandSet : ICommandSet
    {
        private readonly ILogger _logger;
        private readonly ISlackApiClient _slackApiClient;

        public SlackCommandSet(
            ILogger logger,
            ISlackApiClient slackApiClient)
        {
            _logger = logger;
            _slackApiClient = slackApiClient;
        }

        [Command(
            "Ask the borg to join a specific Slack channel",
            "^join slack channel (?<channel>[a-z0-9]+)$")]
        public async Task JoinChannelAsync(
            string channel,
            CancellationToken cancellationToken)
        {
            var apiResponse = await _slackApiClient.CallApiAsync<ApiResponse>(
                "channels.join",
                cancellationToken,
                new KeyValuePair<string, string>("name", channel))
                .ConfigureAwait(false);
            _logger.Debug($"Join channel result {apiResponse}");
        }

        [Command(
            "Ping the borg",
            "^ping (?<message>.*)$")]
        public async Task PingAsync(
            string message,
            SlackMessage slackMessage,
            CancellationToken cancellationToken)
        {
            var apiResponse = await _slackApiClient.SendMessageAsync(
                slackMessage.Channel,
                $"pong {message}",
                cancellationToken)
                .ConfigureAwait(false);
            _logger.Debug($"Message pong result {apiResponse}");
        }
    }
}