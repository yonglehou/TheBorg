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
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TheBorg.Interface.Apis;

namespace TheBorg.Host.Apis
{
    public class HttpApi : IHttpApi
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        public async Task<T> GetAsyncAs<T>(
            Uri uri,
            CancellationToken cancellationToken,
            IEnumerable<KeyValuePair<string, string>> headers = null,
            IEnumerable<HttpStatusCode> validStatusCodes = null)
        {
            var content = await GetAsync(uri, cancellationToken, headers, validStatusCodes).ConfigureAwait(false);

            return JsonConvert.DeserializeObject<T>(content);
        }

        public async Task<string> GetAsync(
            Uri uri,
            CancellationToken cancellationToken,
            IEnumerable<KeyValuePair<string, string>> headers = null,
            IEnumerable<HttpStatusCode> validStatusCodes = null)
        {
            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        httpRequestMessage.Headers.Add(header.Key, header.Value);
                    }
                }

                using (var httpResponseMessage = await HttpClient.GetAsync(uri, cancellationToken).ConfigureAwait(false))
                {
                    if (validStatusCodes == null ||
                        (!validStatusCodes.Contains(httpResponseMessage.StatusCode)))
                    {
                        httpResponseMessage.EnsureSuccessStatusCode();
                    }

                    return await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
            }
        }
    }
}