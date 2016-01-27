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
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Serilog;
using TheBorg.Clients.Slack;

namespace TheBorg.Clients
{
    public class SlackClient : ISlackClient
    {
        private readonly ILogger _logger;
        private readonly IRestClient _restClient;
        private readonly IWebSocketClient _webSocketClient;

        public SlackClient(
            ILogger logger,
            IRestClient restClient,
            IWebSocketClient webSocketClient)
        {
            _logger = logger;
            _restClient = restClient;
            _webSocketClient = webSocketClient;
        }

        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            var rtmStartResponse = await CallApiAsync<RtmStartResponse>(
                "rtm.start",
                new Dictionary<string, string>()
                    {
                        {"token", ""}
                    },
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
    }
}