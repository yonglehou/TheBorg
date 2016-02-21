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
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using TheBorg.Core.Extensions;

namespace TheBorg.PluginManagement
{
    public class PluginInstaller : IPluginInstaller
    {
        private readonly ILogger _logger;

        public PluginInstaller(
            ILogger logger)
        {
            _logger = logger;
        }

        public Task<string> InstallPluginAsync(
            string name,
            string version,
            string path,
            PluginPackageType packageType,
            CancellationToken cancellationToken)
        {
            var serviceDirectory = Path.GetDirectoryName(typeof (PluginInstaller).Assembly.GetCodeBase());
            _logger.Verbose($"Service install location is '{serviceDirectory}'");

            var installDirectory = Path.Combine(serviceDirectory, name, version);
            _logger.Verbose($"Plugin install directory is '{installDirectory}'");
            Directory.CreateDirectory(installDirectory);

            switch (packageType)
            {
                case PluginPackageType.Zip:
                    {
                        ZipFile.ExtractToDirectory(path, installDirectory);
                        var pluginDll = Path.Combine(installDirectory, $"{name}.dll");
                        _logger.Verbose($"Guessing that plugin location is '{pluginDll}'");
                        return Task.FromResult(pluginDll);
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(packageType), packageType, null);
            }
        }
    }
}