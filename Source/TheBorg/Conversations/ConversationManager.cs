// The MIT License(MIT)
// 
// Copyright(c) 2016 Rasmus Mikkelsen
// https://github.com/theborgbot/TheBorg
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

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using TheBorg.Commands;
using TheBorg.Core;
using TheBorg.Interface.ValueObjects;

namespace TheBorg.Conversations
{
    public class ConversationManager : IConversationManager
    {
        private readonly ITime _time;
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ICommandBuilder _commandBuilder;
        private readonly ConcurrentDictionary<Address, ActiveConversation> _activeConversations = new ConcurrentDictionary<Address, ActiveConversation>();
        private readonly IReadOnlyDictionary<Regex, IConversationTopic> _conversationTopics;

        private static readonly IReadOnlyCollection<Regex> ConversationEnders = new[]
            {
                "^done$",
                "^nevermind$",
            }
            .Select(s => new Regex(s, RegexOptions.Compiled | RegexOptions.IgnoreCase))
            .ToList();

        public ConversationManager(
            ITime time,
            ILogger logger,
            IJsonSerializer jsonSerializer,
            ICommandBuilder commandBuilder,
            IEnumerable<IConversationTopic> conversationTopics)
        {
            _time = time;
            _logger = logger;
            _jsonSerializer = jsonSerializer;
            _commandBuilder = commandBuilder;
            _conversationTopics = CreateTopicIndex(conversationTopics);
        }

        public async Task<ProcessMessageResult> ProcessAsync(TenantMessage tenantMessage, CancellationToken cancellationToken)
        {
            var processMessageResult = ProcessMessageResult.Skipped;
            ActiveConversation activeConversation;
            if (_activeConversations.TryGetValue(tenantMessage.Address, out activeConversation))
            {
                if (ConversationEnders.Any(e => e.IsMatch(tenantMessage.Text)))
                {
                    _activeConversations.TryRemove(tenantMessage.Address, out activeConversation);
                    await activeConversation.EndAsync(cancellationToken).ConfigureAwait(false);
                    await tenantMessage.ReplyAsync("Ok, lets talk about some else...", cancellationToken).ConfigureAwait(false);
                    return ProcessMessageResult.Handled;
                }

                processMessageResult = await activeConversation.ProcessAsync(tenantMessage, cancellationToken).ConfigureAwait(false);
                if (processMessageResult == ProcessMessageResult.Handled)
                {
                    return ProcessMessageResult.Handled;
                }
            }

            foreach (var kv in _conversationTopics)
            {
                if (kv.Key.IsMatch(tenantMessage.Text))
                {
                    activeConversation = new ActiveConversation(
                        tenantMessage.Address,
                        _time,
                        _jsonSerializer,
                        kv.Value,
                        _commandBuilder);
                    if (!_activeConversations.TryAdd(activeConversation.Recipient, activeConversation))
                    {
                        _logger.Error("Failed to start conversation");
                        await tenantMessage.ReplyAsync("Failed to start your conversation", cancellationToken).ConfigureAwait(false);
                        return ProcessMessageResult.Handled;
                    }

                    await kv.Value.StartAsync(tenantMessage, activeConversation, cancellationToken).ConfigureAwait(false);
                    return ProcessMessageResult.Handled;
                }
            }

            return processMessageResult;
        }

        private static IReadOnlyDictionary<Regex, IConversationTopic> CreateTopicIndex(IEnumerable<IConversationTopic> conversationTopics)
        {
            var topicIndex = new Dictionary<Regex, IConversationTopic>();
            foreach (var conversationTopic in conversationTopics)
            {
                var conversationTopicType = conversationTopic.GetType();
                var conversationTopicAttribute = (ConversationTopicAttribute) conversationTopicType
                    .GetCustomAttributes(typeof (ConversationTopicAttribute), false)
                    .Single();
                var regex = new Regex(conversationTopicAttribute.StartedBy, RegexOptions.Compiled);
                topicIndex.Add(regex, conversationTopic);
            }
            return topicIndex;
        }
    }
}