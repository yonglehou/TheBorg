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
using System.Threading;
using System.Threading.Tasks;
using Octokit;
using TheBorg.Interface.Apis;

namespace TheBorg.Plugins.GitHub.Services
{
    public class GitHubClientFactory : IGitHubClientFactory
    {
        private readonly IConfigApi _configApi;
        private IGitHubClient _gitHubClient;

        public GitHubClientFactory(
            IConfigApi configApi)
        {
            _configApi = configApi;
        }

        public async Task<IGitHubClient> GetClientAsync(CancellationToken cancellationToken)
        {
            if (_gitHubClient != null)
            {
                return _gitHubClient;
            }

            var token = await _configApi.GetAsync("github-token", cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(token))
            {
                token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
            }

            var productHeaderValue = new ProductHeaderValue(
                "TheBorg.Plugins.GitHub",
                GetType().Assembly.GetName().Version.ToString());

            _gitHubClient = new GitHubClient(productHeaderValue)
                {
                    Credentials = new Credentials(token)
                };
            return _gitHubClient;
        }
    }
}