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
using System.Linq;

namespace TheBorg.Tests
{
    public static class TestDataReader
    {
        public static Stream OpenEmbeddedResourceStream(string endWith)
        {
            var assembly = typeof (TestDataReader).Assembly;
            var resourceNames = assembly.GetManifestResourceNames();
            var resourceName = resourceNames.SingleOrDefault(r => r.EndsWith(endWith));

            if (string.IsNullOrEmpty(resourceName))
            {
                throw new ArgumentException($"Not found '{endWith}': {string.Join(", ", resourceNames)}");
            }

            return assembly.GetManifestResourceStream(resourceName);
        }

        public static string ExtractEmbeddedResource(string endsWith)
        {
            var directoryPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            var filePath = Path.Combine(directoryPath, endsWith);

            Directory.CreateDirectory(directoryPath);

            using (var embeddedResourceStream = OpenEmbeddedResourceStream(endsWith))
            using (var fileStream = File.OpenWrite(filePath))
            {
                embeddedResourceStream.CopyTo(fileStream);
            }

            return filePath;
        }
    }
}