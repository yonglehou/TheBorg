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
using FluentAssertions;
using NUnit.Framework;
using Serilog;
using TheBorg.Common.Clients;

namespace TheBorg.Tests.UnitTests.Common.Clients
{
    public class FileSystemClientTests
    {
        private IFileSystemClient _sut;

        [SetUp]
        public void SetUp()
        {
            _sut = new FileSystemClient(
                new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Console().CreateLogger());
        }

        [Test]
        public void ExtractZipAsync()
        {
            // Arrange
            var zipFile = TestDataReader.ExtractEmbeddedResource("theborg.zip");
            var destinationDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

            // Act
            _sut.ExtractZipAsync(zipFile, destinationDirectory, CancellationToken.None).Wait();

            // Assert
            var expectedDestinationFile = Path.Combine(destinationDirectory, "theborg", "theborg.txt");
            File.Exists(expectedDestinationFile).Should().BeTrue();
            var content = File.ReadAllText(expectedDestinationFile);
            content.Should().Be("resistance is futile");
        }
    }
}