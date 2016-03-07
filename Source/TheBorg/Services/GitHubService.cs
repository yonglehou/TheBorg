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
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Octokit;
using TheBorg.ValueObjects;

namespace TheBorg.Services
{
    public class GitHubService : IGitHubService
    {
        private static readonly GitHubClient GitHubClient = new GitHubClient(new ProductHeaderValue(
            "TheBorg",
            typeof(GitHubService).Assembly.GetName().Version.ToString()));

        static GitHubService()
        {
            var token = ConfigurationManager.AppSettings["theborg:github.token"] ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN");
            GitHubClient.Credentials = new Credentials(token);
        }

        public async Task<IReadOnlyCollection<GitHubRelease>> GetAllReleasesAsync(
            string owner,
            string name,
            CancellationToken cancellationToken)
        {
            var allRelases = await GitHubClient.Repository.Release.GetAll(owner, name).ConfigureAwait(false);
            return CreateReleases(allRelases).ToList();
        }

        private static IEnumerable<GitHubRelease> CreateReleases(IEnumerable<Release> releases)
        {
            return releases.Select(r => new GitHubRelease(
                r.Prerelease,
                r.TagName,
                r.Name,
                CreateReleaseAssets(r.Assets)));
        }

        private static IEnumerable<GitHubReleaseAsset> CreateReleaseAssets(IEnumerable<ReleaseAsset> releaseAssets)
        {
            return releaseAssets.Select(a => new GitHubReleaseAsset(
                a.Name,
                new Uri(a.BrowserDownloadUrl, UriKind.Absolute)));
        }
    }
}