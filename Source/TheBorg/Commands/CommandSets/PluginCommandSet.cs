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

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using TheBorg.Interface.ValueObjects;
using TheBorg.Services;

namespace TheBorg.Commands.CommandSets
{
    public class PluginCommandSet : ICommandSet
    {
        private readonly IPluginService _pluginService;

        public PluginCommandSet(
            IPluginService pluginService)
        {
            _pluginService = pluginService;
        }

        [Command(
            "load plugin <plugin name> - loads a plugin",
            @"^load plugin (?<pluginName>[\.a-z0-9]+)$")]
        public async Task LoadPluginAsync(string pluginName, TenantMessage tenantMessage, CancellationToken cancellationToken)
        {
            await tenantMessage.ReplyAsync($"Loading plugin '{pluginName}'", cancellationToken).ConfigureAwait(false);
            var stopwatch = Stopwatch.StartNew();
            await _pluginService.LoadPluginAsync(pluginName).ConfigureAwait(false);
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
    }
}