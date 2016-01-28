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
using System.Threading;
using System.Threading.Tasks;
using TheBorg.Commands;
using TheBorg.Core;
using TheBorg.ValueObjects;

namespace TheBorg.Conversations
{
    public class ActiveConversation : IActiveConversation
    {
        private readonly IConversationTopic _conversationTopic;
        private readonly ConcurrentDictionary<string, object> _keyValueStore = new ConcurrentDictionary<string, object>(); 

        public ActiveConversation(
            Address with,
            ITime time,
            IConversationTopic conversationTopic)
        {
            With = with;
            Started = time.Now;

            _conversationTopic = conversationTopic;
        }

        public DateTimeOffset Started { get; }
        public Address With { get; }
        public bool TryGet<T>(string key, out T value)
        {
            object obj;

            if (!_keyValueStore.TryGetValue(key, out obj))
            {
                value = default(T);
                return false;
            }

            value = (T) obj;
            return true;
        }

        public bool TryAdd<T>(string key, T value)
        {
            return _keyValueStore.TryAdd(key, value);
        }

        public Task<ProcessMessageResult> ProcessAsync(TenantMessage tenantMessage, CancellationToken cancellationToken)
        {
            return Task.FromResult(ProcessMessageResult.Skipped);
        }
    }
}