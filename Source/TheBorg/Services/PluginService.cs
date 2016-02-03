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
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TheBorg.Extensions;
using TheBorg.Plugins;

namespace TheBorg.Services
{
    public class PluginService : IPluginService
    {
        private readonly IPluginLoader _pluginLoader;
        private readonly Dictionary<string, IPluginProxy> _plugins = new Dictionary<string, IPluginProxy>(); 

        public PluginService(
            IPluginLoader pluginLoader)
        {
            _pluginLoader = pluginLoader;
        }

        public async Task LoadPluginAsync(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            var pluginPath = name;
            if (!Path.IsPathRooted(name))
            {
                name = name.ToLowerInvariant().Trim();
                if (!name.EndsWith(".dll"))
                {
                    name += ".dll";
                }

                pluginPath = Path.Combine(
                    Path.GetDirectoryName(typeof(PluginService).Assembly.GetCodeBase()),
                    name);
            }

            var plugin = await _pluginLoader.LoadPluginAsync(pluginPath).ConfigureAwait(false);

            _plugins.Add(name, plugin);
        }

        public Task UnloadPluginAsync(string name)
        {
            name = name.ToLowerInvariant().Trim();

            if (!_plugins.ContainsKey(name))
            {
                return Task.FromResult(0);
            }

            _plugins.Remove(name);
            var pluginProxy = _plugins[name];
            pluginProxy.Dispose();

            return Task.FromResult(0);
        }
    }
}