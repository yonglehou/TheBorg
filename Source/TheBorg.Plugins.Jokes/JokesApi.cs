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
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using TheBorg.Interface;
using TheBorg.Interface.Apis;
using TheBorg.Interface.Attributes;
using TheBorg.Interface.ValueObjects;

namespace TheBorg.Plugins.Jokes
{
    public class JokesApi : IPluginHttpApi
    {
        private readonly IConfigApi _configApi;
        private readonly IHttpApi _httpApi;
        private readonly IMessageApi _messageApi;

        public JokesApi(
            IConfigApi configApi,
            IHttpApi httpApi,
            IMessageApi messageApi)
        {
            _configApi = configApi;
            _httpApi = httpApi;
            _messageApi = messageApi;
        }

        [HttpApi(HttpApiMethod.Get, "api/joke")]
        public async Task<string> Joke(CancellationToken cancellationToken)
        {
            var uriStr = await _configApi.GetAsync(ConfigKey.With("api-uri"), cancellationToken).ConfigureAwait(false);
            var uri = new Uri(uriStr, UriKind.Absolute);

            var jokeContainer = await _httpApi.GetAsyncAs<JokeContainer>(uri, cancellationToken).ConfigureAwait(false);
            return jokeContainer.Value.Joke;
        }

        [HttpApi(HttpApiMethod.Post, "api/commands/joke")]
        [Command("^joke$", "tells a joke")]
        public async Task TellJoke([FromBody]TenantMessage tenantMessage, CancellationToken cancellationToken)
        {
            var joke = await Joke(cancellationToken).ConfigureAwait(false);
            await _messageApi.SendAsync(tenantMessage.CreateReply(joke), CancellationToken.None).ConfigureAwait(false);
        }

        public class JokeValue
        {
            public JokeValue(
                int id,
                string joke)
            {
                Id = id;
                Joke = HttpUtility.HtmlDecode(joke);
            }

            public int Id { get; }
            public string Joke { get; }
        }

        public class JokeContainer
        {
            public JokeContainer(
                string type,
                JokeValue value)
            {
                Type = type;
                Value = value;
            }

            public string Type { get; }
            public JokeValue Value { get; }
        }
    }
}