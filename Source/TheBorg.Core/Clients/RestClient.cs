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
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Serilog;
using TheBorg.Core.Extensions;
using TheBorg.Core.Serialization;

namespace TheBorg.Core.Clients
{
    public class RestClient : IRestClient
    {
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;
        private static readonly HttpClient HttpClient = new HttpClient();

        public RestClient(
            ILogger logger,
            IJsonSerializer jsonSerializer)
        {
            _logger = logger;
            _jsonSerializer = jsonSerializer;
        }

        public Task<T> GetAsync<T>(
            Uri uri,
            JsonFormat jsonFormat,
            CancellationToken cancellationToken)
        {
            return GetAsync<T>(
                uri,
                Enumerable.Empty<KeyValuePair<string, string>>(),
                jsonFormat,
                cancellationToken);
        }

        public async Task<T> GetAsync<T>(
            Uri uri,
            IEnumerable<KeyValuePair<string, string>> queryString,
            JsonFormat jsonFormat,
            CancellationToken cancellationToken)
        {
            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri.WithQueryString(queryString)))
            using (var httpResponseMessage = await SendAsync(httpRequestMessage, cancellationToken).ConfigureAwait(false))
            {
                httpResponseMessage.EnsureSuccessStatusCode();
                var json = await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                return _jsonSerializer.Deserialize<T>(json, jsonFormat);
            }
        }

        public async Task<T> PostFormAsync<T>(
            Uri uri,
            IEnumerable<KeyValuePair<string, string>> keyValuePairs,
            JsonFormat jsonFormat,
            CancellationToken cancellationToken)
        {
            var formUrlEncodedContent = new FormUrlEncodedContent(keyValuePairs);

            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri) { Content = formUrlEncodedContent, })
            using (var httpResponseMessage = await SendAsync(httpRequestMessage, cancellationToken).ConfigureAwait(false))
            {
                var json = await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                return _jsonSerializer.Deserialize<T>(json, jsonFormat);
            }
        }

        public async Task<TResult> PostAsync<TResult, T>(
            Uri uri,
            T obj,
            JsonFormat jsonFormat,
            CancellationToken cancellationToken)
        {
            var requestJson = JsonConvert.SerializeObject(obj);
            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri))
            {
                httpRequestMessage.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");
                using (var httpResponseMessage = await SendAsync(httpRequestMessage, cancellationToken).ConfigureAwait(false))
                {
                    var responseJson = await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return _jsonSerializer.Deserialize<TResult>(responseJson, jsonFormat);
                }
            }
        }

        public async Task<TempFile> DownloadAsync(
            Uri uri,
            CancellationToken cancellationToken)
        {
            var tempFile = TempFile.New;

            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri))
            using (var httpResponseMessage = await SendAsync(httpRequestMessage, cancellationToken).ConfigureAwait(false))
            using (var fileStream = new FileStream(tempFile.Path, FileMode.CreateNew))
            using (var responseStream = await httpResponseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false))
            {
                await responseStream.CopyToAsync(fileStream).ConfigureAwait(false);
            }

            return tempFile;
        }

        public Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage httpRequestMessage,
            CancellationToken cancellationToken)
        {
            _logger.Verbose($"HTTP: {httpRequestMessage.Method} {httpRequestMessage.RequestUri.GetLeftPart(UriPartial.Path)}");

            httpRequestMessage.Headers.UserAgent.Add(new ProductInfoHeaderValue(new ProductHeaderValue("TheBorg", typeof(RestClient).Assembly.GetName().Version.ToString())));
            return HttpClient.SendAsync(httpRequestMessage, cancellationToken);
        }
    }
}