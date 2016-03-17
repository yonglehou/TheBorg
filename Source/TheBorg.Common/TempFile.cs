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
using System.Threading;
using System.Threading.Tasks;
using TheBorg.Common.Clients;

namespace TheBorg.Common
{
    public class TempFile : IDisposable
    {
        private readonly string _filePath;
        private readonly IFileSystemClient _fileSystemClient;

        private TempFile(
            string filePath,
            IFileSystemClient fileSystemClient)
        {
            _fileSystemClient = fileSystemClient;
            _filePath = filePath;
        }

        public void Dispose()
        {
            _fileSystemClient.DeleteFile(_filePath);
        }

        public Task WriteAsync(Stream stream, CancellationToken cancellationToken)
        {
            return _fileSystemClient.WriteAsync(stream, _filePath, cancellationToken);
        }

        public Task ExtractZipAsync(string destinationDirectory, CancellationToken cancellationToken)
        {
            return _fileSystemClient.ExtractZipAsync(_filePath, destinationDirectory, cancellationToken);
        }

        public static TempFile New(IFileSystemClient fileSystemClient) => new TempFile(
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")),
            fileSystemClient);
    }
}