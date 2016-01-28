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
using Newtonsoft.Json;
using TheBorg.Clients;
using TheBorg.ValueObjects;
using TheBorg.Commands.Attributes;

namespace TheBorg.Commands.CommandSets
{
    public class ChuckNorrisCommandSet : ICommandSet
    {
        private readonly IRestClient _restClient;

        public ChuckNorrisCommandSet(
            IRestClient restClient)
        {
            _restClient = restClient;
        }

        [Command(
            "Ask the borg to tell a joke",
            "^joke|tell a joke$")]
        public async Task TellJokeAsync(TenantMessage tenantMessage, CancellationToken cancellationToken)
        {
            var json = await _restClient.GetAsync(
                new Uri("http://api.icndb.com/jokes/random"),
                cancellationToken)
                .ConfigureAwait(false);
            var joke = JsonConvert.DeserializeObject<JokeContainer>(json);
            await tenantMessage.ReplyAsync(joke.Value.Joke, cancellationToken).ConfigureAwait(false);
        }

        public class JokeValue
        {
            public JokeValue(
                int id,
                string joke)
            {
                Id = id;
                Joke = joke;
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