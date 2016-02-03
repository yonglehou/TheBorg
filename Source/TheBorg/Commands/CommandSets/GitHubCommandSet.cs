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

using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TheBorg.Interface.ValueObjects;
using TheBorg.Services;

namespace TheBorg.Commands.CommandSets
{
    public class GitHubCommandSet : ICommandSet
    {
        private readonly IGitHubService _gitHubService;

        public GitHubCommandSet(
            IGitHubService gitHubService)
        {
            _gitHubService = gitHubService;
        }

        [Command(
            "list releases for <owner>/<repo>",
            @"^list releases for (?<owner>[\.a-z\-0-9]+)/(?<repo>[\.a-z\-0-9]+)$")]
        public async Task ListReleasesAsync(
            string owner,
            string repo,
            TenantMessage tenantMessage,
            CancellationToken cancellationToken)
        {
            await tenantMessage.ReplyAsync($"Finding all relases for {owner}/{repo}", cancellationToken).ConfigureAwait(false);
            var releases = await _gitHubService.GetAllReleasesAsync(owner, repo, cancellationToken).ConfigureAwait(false);
            if (!releases.Any())
            {
                await tenantMessage.ReplyAsync($"There's no releases for {owner}/{repo}", cancellationToken).ConfigureAwait(false);
                return;
            }

            var stringBuilder = releases.Aggregate(
                new StringBuilder().AppendLine($"There's these releases for {owner}/{repo}"),
                (s, r) => s.AppendLine($"{r.Name} [{r.TagName}]"));
            await tenantMessage.ReplyAsync(stringBuilder.ToString(), cancellationToken).ConfigureAwait(false);
        }
    }
}