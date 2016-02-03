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

using System.Threading;
using System.Threading.Tasks;
using TheBorg.Commands;
using TheBorg.Interface.ValueObjects;

namespace TheBorg.Conversations.Topics
{
    [ConversationTopic(
        "Octopus releases",
        "^create octopus release$")]
    public class OctopusCreateReleaseTopic : IConversationTopic
    {
        public async Task StartAsync(
            TenantMessage tenantMessage,
            IActiveConversation activeConversation,
            CancellationToken cancellationToken)
        {
            await tenantMessage.ReplyAsync("Ok, lets create a release", cancellationToken).ConfigureAwait(false);
        }

        public Task EndAsync(IActiveConversation activeConversation, CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }

        [Command(
            "Set the project to create a release for",
            @"^project is (?<project>[a-zA-Z0-9\-]+)$")]
        public async Task SetProjectAsync(
            string project,
            TenantMessage tenantMessage,
            IActiveConversation activeConversation,
            CancellationToken cancellationToken)
        {
            activeConversation.TrySet("project-name", project);

            // TODO: Validate that the project actally exists

            await tenantMessage.ReplyAsync($"Ok, I'll use the project '{project}' for the release", cancellationToken).ConfigureAwait(false);
        }
    }
}