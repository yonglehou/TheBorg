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
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TheBorg.Interface.ValueObjects.Plugins;

namespace TheBorg.Host.Apis
{
    public abstract class Api
    {
        private readonly PluginId _pluginId;
        private readonly Uri _baseUri;
        private static readonly HttpClient HttpClient = new HttpClient();

        protected Api(
            PluginId pluginId,
            Uri baseUri)
        {
            _pluginId = pluginId;
            _baseUri = baseUri;
        }

        protected async Task PostAsync<T>(
            string path,
            T value,
            CancellationToken cancellationToken)
        {
            var httpContent = BuildJsonContent(value);
            using (var httpResponseMessage = await SendAsync(
                path,
                HttpMethod.Post,
                httpContent,
                cancellationToken))
            {
                httpResponseMessage.EnsureSuccessStatusCode();
            }
        }

        private static HttpContent BuildJsonContent<T>(T value)
        {
            var json = JsonConvert.SerializeObject(value);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        protected async Task<HttpResponseMessage> SendAsync(
            string path,
            HttpMethod httpMethod,
            HttpContent httpContent,
            CancellationToken cancellationToken)
        {
            using (var httpRequestMessage = new HttpRequestMessage(httpMethod, BuildUri(path))
                {
                    Content = httpContent,
                })
            {
                return await SendAsync(httpRequestMessage, cancellationToken).ConfigureAwait(false);
            }
        }

        private Uri BuildUri(string path)
        {
            return new Uri(_baseUri, path);
        }

        protected Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage httpRequestMessage,
            CancellationToken cancellationToken)
        {
            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("plugin.id", _pluginId.Value);
            return HttpClient.SendAsync(httpRequestMessage, cancellationToken);
        }
    }
}