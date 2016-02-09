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
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using TheBorg.Core;
using TheBorg.Interface.ValueObjects;

namespace TheBorg.Plugins
{
    public class Plugin : IPlugin
    {
        private readonly ILogger _logger;
        private readonly Uri _baseUri;
        private readonly IRestClient _restClient;

        public Plugin(
            ILogger logger,
            Uri baseUri,
            IRestClient restClient)
        {
            _logger = logger;
            _baseUri = baseUri;
            _restClient = restClient;
        }

        public Task PingAsync(CancellationToken cancellationToken)
        {
            return GetAsync<object>("_plugin/ping", cancellationToken);
        }

        public Task<PluginInformation> GetPluginInformationAsync(CancellationToken cancellationToken)
        {
            return GetAsync<PluginInformation>("_plugin/plugin-information", cancellationToken);
        }

        private Task<T> GetAsync<T>(string path, CancellationToken cancellationToken)
        {
            return _restClient.GetAsync<T>(new Uri(_baseUri, path), JsonFormat.CamelCase, cancellationToken);
        }
    }
}
