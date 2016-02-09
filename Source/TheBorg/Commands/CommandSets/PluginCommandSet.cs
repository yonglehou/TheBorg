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

using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TheBorg.Core;
using TheBorg.Interface.ValueObjects;
using TheBorg.Plugins;
using TheBorg.Services;

namespace TheBorg.Commands.CommandSets
{
    public class PluginCommandSet : ICommandSet
    {
        private readonly IRestClient _restClient;
        private readonly IGitHubService _gitHubService;
        private readonly IPluginService _pluginService;
        private readonly IPluginInstaller _pluginInstaller;

        public PluginCommandSet(
            IRestClient restClient,
            IGitHubService gitHubService,
            IPluginService pluginService,
            IPluginInstaller pluginInstaller)
        {
            _restClient = restClient;
            _gitHubService = gitHubService;
            _pluginService = pluginService;
            _pluginInstaller = pluginInstaller;
        }

        [Command(
            "load plugin <plugin name> - loads a plugin",
            @"^load plugin (?<pluginName>[\.a-z0-9]+)$")]
        public async Task LoadPluginAsync(string pluginName, TenantMessage tenantMessage, CancellationToken cancellationToken)
        {
            await tenantMessage.ReplyAsync($"Loading plugin '{pluginName}'", cancellationToken).ConfigureAwait(false);
            var stopwatch = Stopwatch.StartNew();
            await _pluginService.LoadPluginAsync(pluginName, cancellationToken).ConfigureAwait(false);
            var time = stopwatch.Elapsed;
            await tenantMessage.ReplyAsync($"It took me {time.TotalSeconds:0.00} seconds to load '{pluginName}'", cancellationToken).ConfigureAwait(false);
        }

        [Command(
            "unload plugin <plugin name> - loads a plugin",
            @"^unload plugin (?<pluginName>[\.a-z0-9]+)$")]
        public async Task UnloadPluginAsync(string pluginName, TenantMessage tenantMessage, CancellationToken cancellationToken)
        {
            await tenantMessage.ReplyAsync($"Unloading plugin '{pluginName}'", cancellationToken).ConfigureAwait(false);
            var stopwatch = Stopwatch.StartNew();
            await _pluginService.UnloadPluginAsync(pluginName).ConfigureAwait(false);
            var time = stopwatch.Elapsed;
            await tenantMessage.ReplyAsync($"It took me {time.TotalSeconds:0.00} seconds to unload '{pluginName}'", cancellationToken).ConfigureAwait(false);
        }

        [Command(
            "install plugin from github <owner>/<repo>",
            @"^install plugin from github (?<owner>[\.a-z\-0-9]+)/(?<repo>[\.a-z\-0-9]+)$")]
        public async Task InstallPluginFromGitHubAsync(
            string owner,
            string repo,
            TenantMessage tenantMessage,
            CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            await tenantMessage.ReplyAsync($"Finding all relases for {owner}/{repo}", cancellationToken).ConfigureAwait(false);
            var releases = (await _gitHubService.GetAllReleasesAsync(owner, repo, cancellationToken).ConfigureAwait(false))
                .Take(5)
                .ToList();
            if (!releases.Any())
            {
                await tenantMessage.ReplyAsync($"There's no releases for {owner}/{repo}, stopping install!", cancellationToken).ConfigureAwait(false);
                return;
            }

            var stringBuilder = releases.Aggregate(
                new StringBuilder().AppendLine($"This is the first {releases.Count} releases for {owner}/{repo}"),
                (s, r) => s.AppendLine($"{r.Name} [{r.TagName}]"));
            var release = releases[0];
            stringBuilder.AppendLine($"Picking {release.Name} [{release.TagName}] to install");
            await tenantMessage.ReplyAsync(stringBuilder.ToString(), cancellationToken).ConfigureAwait(false);

            // TODO: Better searching for the right file!
            var releaseAsset = release.ReleaseAssets.Single(a => a.Uri.ToString().Contains("TheBorg.Plugins"));

            using (var tempFile = await _restClient.DownloadAsync(releaseAsset.Uri, cancellationToken).ConfigureAwait(false))
            {
                var dllLocation = await _pluginInstaller.InstallPluginAsync(
                    releaseAsset.Name.Replace(".zip", string.Empty),
                    release.Name,
                    tempFile.Path,
                    PluginPackageType.Zip,
                    cancellationToken)
                    .ConfigureAwait(false);

                await _pluginService.LoadPluginAsync(
                    dllLocation,
                    cancellationToken)
                    .ConfigureAwait(false);
            }

            stopwatch.Stop();
            await tenantMessage.ReplyAsync(
                $"Finished installing plugin {owner}/{repo} - {release.Name} [{release.TagName}] in {stopwatch.Elapsed.TotalSeconds:0.00} seconds",
                cancellationToken)
                .ConfigureAwait(false);
        }
    }
}