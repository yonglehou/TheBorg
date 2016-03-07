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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheBorg.Interface;
using TheBorg.Interface.Attributes;
using TheBorg.Interface.ValueObjects;
using TheBorg.Interface.ValueObjects.Plugins;

namespace TheBorg.Host
{
    public class PluginApi : IPluginHttpApi
    {
        private readonly PluginInformation _pluginInformation;
        private readonly IReadOnlyCollection<CommandDescription> _commandDescriptions;

        public PluginApi(
            PluginInformation pluginInformation,
            IReadOnlyCollection<CommandDescription> commandDescriptions)
        {
            _pluginInformation = pluginInformation;
            _commandDescriptions = commandDescriptions;
        }

        [HttpApi(HttpApiMethod.Get, "_plugin/ping")]
        public Task<string> Ping(IHttpApiRequestContext httpApiRequestContext, CancellationToken cancellationToken)
        {
            return Task.FromResult("");
        }

        [HttpApi(HttpApiMethod.Get, "_plugin/plugin-information")]
        public Task<PluginInformation> PluginInformation()
        {
            return Task.FromResult(_pluginInformation);
        }

        [HttpApi(HttpApiMethod.Get, "_plugin/commands")]
        public Task<IReadOnlyCollection<CommandDescription>> ListCommands()
        {
            return Task.FromResult(_commandDescriptions);
        }
    }
}