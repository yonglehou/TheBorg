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
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using TheBorg.Interface.ValueObjects;

namespace TheBorg.Common.Clients
{
    public class FileSystemClient : IFileSystemClient
    {
        private const int DefaultBufferSize = 4094;

        private readonly ILogger _logger;

        public FileSystemClient(
            ILogger logger)
        {
            _logger = logger;
        }

        public void DeleteFile(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            _logger.Verbose($"FILE DELETE: {path}");
            File.Delete(path);
        }

        public async Task WriteAsync(Stream sourceStream, string destinationFile, CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));

            using (var fileStream = new FileStream(destinationFile, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await sourceStream.CopyToAsync(fileStream, DefaultBufferSize, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<DataSize> ExtractZipAsync(string sourceFile, string destinationDirectory, CancellationToken cancellationToken)
        {
            var fullNameDestinationDirectory = Directory.CreateDirectory(destinationDirectory).FullName;
            long dataSize = 0;

            using (var zipArchive = ZipFile.OpenRead(sourceFile))
            {
                foreach (var source in zipArchive.Entries)
                {
                    var fullPath = Path.GetFullPath(Path.Combine(fullNameDestinationDirectory, source.FullName));

                    if (!fullPath.StartsWith(fullNameDestinationDirectory, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Extracting file outside directory");
                    }

                    if (Path.GetFileName(fullPath).Length == 0)
                    {
                        if (source.Length != 0L)
                        {
                            throw new InvalidOperationException("Directory with data");
                        }

                        Directory.CreateDirectory(fullPath);
                    }
                    else
                    {
                        using (var stream = source.Open())
                        {
                            await WriteAsync(stream, fullPath, cancellationToken).ConfigureAwait(false);
                        }
                        _logger.Verbose($"ZIP EXTRACT: {fullPath}");

                        File.SetLastWriteTime(fullPath, source.LastWriteTime.DateTime);
                        dataSize += source.Length;
                    }
                }
            }

            return DataSize.With(dataSize);
        }
    }
}