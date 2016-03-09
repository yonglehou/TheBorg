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
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Serilog;
using TheBorg.Core;

namespace TheBorg.Services.Update
{
    public class TheBorgDownloader : ITheBorgDownloader
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private static readonly HttpClient RedirectReaderHttpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, });
        private static readonly HttpClient HttpClient = new HttpClient();

        public TheBorgDownloader(
            ILogger logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task DownloadFile(string url, string targetFile, Action<int> progress)
        {
            _logger.Debug($"Downloading '{url}' to {targetFile}");

            using (var sourceStream = await HttpClient.GetStreamAsync(url).ConfigureAwait(false))
            using (var fileStream = new FileStream(targetFile, FileMode.CreateNew))
            {
                await sourceStream.CopyToAsync(fileStream).ConfigureAwait(false);
            }
        }

        public async Task<byte[]> DownloadUrl(string url)
        {
            _logger.Debug($"Requesting to download '{url}'");

            var fakeUri = new Uri(url, UriKind.Absolute);
            if (!fakeUri.AbsolutePath.EndsWith("/RELEASES"))
            {
                throw new ArgumentException($"Cannot understand '{url}' as it doesn't end with '/RELEASES'", nameof(url));
            }

            var uri = await GetReleaseUrlAsync().ConfigureAwait(false);
            var newUri = new Uri($"{uri}{fakeUri.AbsolutePath}".Replace("/tag/", "/download/"), UriKind.Absolute);

            return await HttpClient.GetByteArrayAsync(newUri).ConfigureAwait(false);
        }

        private async Task<Uri> GetReleaseUrlAsync()
        {
            using (var httpResponseMessage = await RedirectReaderHttpClient.GetAsync(_configuration.LatestReleasesUri).ConfigureAwait(false))
            {
                if (httpResponseMessage.Headers.Location == null)
                {
                    throw new Exception();
                }

                return httpResponseMessage.Headers.Location;
            }
        }
    }
}