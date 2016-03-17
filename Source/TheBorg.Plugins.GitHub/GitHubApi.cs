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

using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TheBorg.Interface;
using TheBorg.Interface.Apis;
using TheBorg.Interface.Attributes;
using TheBorg.Interface.ValueObjects;
using TheBorg.Interface.ValueObjects.Tenants;
using TheBorg.Plugins.GitHub.Services;

namespace TheBorg.Plugins.GitHub
{
    public class GitHubApi : IPluginHttpApi
    {
        private readonly IMessageApi _messageApi;
        private readonly IGitHubService _gitHubService;

        public GitHubApi(
            IMessageApi messageApi,
            IGitHubService gitHubService)
        {
            _messageApi = messageApi;
            _gitHubService = gitHubService;
        }

        private static readonly Regex ListReleasesRegex = new Regex("^github releases (?<owner>.*?)/(?<repo>.*?)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        [HttpApi(HttpApiMethod.Post, "api/commands/releases/list")]
        [Command("^github releases (?<owner>.*?)/(?<repo>.*?)$", "lists repo GitHub releases")]
        public async Task GetReleases([FromBody] TenantMessage tenantMessage, CancellationToken cancellationToken)
        {
            var match = ListReleasesRegex.Match(tenantMessage.Text);
            var owner = match.Groups["owner"].Value.Trim();
            var repo = match.Groups["repo"].Value.Trim();

            var releases = await _gitHubService.GetAllReleasesAsync(owner, repo, cancellationToken).ConfigureAwait(false);
            if (!releases.Any())
            {
                await _messageApi.ReplyToAsync(tenantMessage, $"There's no releases for {owner}/{repo}", cancellationToken).ConfigureAwait(false);
                return;
            }

            var text = releases.Aggregate(
                new StringBuilder().AppendLine($"Releases for {owner}/{repo}"),
                (b, r) => b.AppendLine($"- {r.Name} [{r.TagName}]"))
                .ToString();

            await _messageApi.ReplyToAsync(tenantMessage, text, cancellationToken).ConfigureAwait(false);
        }
    }
}