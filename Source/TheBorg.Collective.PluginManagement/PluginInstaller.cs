﻿// The MIT License(MIT)
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
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using TheBorg.Common;
using TheBorg.Common.Clients;
using TheBorg.Interface.ValueObjects.Plugins;

namespace TheBorg.Collective.PluginManagement
{
    public class PluginInstaller : IPluginInstaller
    {
        private readonly ILogger _logger;
        private readonly IFileSystemClient _fileSystemClient;
        private readonly IConfiguration _configuration;

        public PluginInstaller(
            ILogger logger,
            IFileSystemClient fileSystemClient,
            IConfiguration configuration)
        {
            _logger = logger;
            _fileSystemClient = fileSystemClient;
            _configuration = configuration;
        }

        public async Task<PluginPath> InstallPluginAsync(
            PluginId pluginId,
            TempFile tempFile,
            PluginPackageType packageType,
            CancellationToken cancellationToken)
        {
            var installDirectory = GetInstallDirectory(pluginId);

            _logger.Verbose($"Installing plugin '{pluginId}' into '{installDirectory}'");

            switch (packageType)
            {
                case PluginPackageType.Zip:
                    {
                        await tempFile.ExtractZipAsync(installDirectory, cancellationToken).ConfigureAwait(false);
                        var pluginDll = Path.Combine(installDirectory, $"{pluginId}.dll");
                        _logger.Verbose($"Guessing that plugin location is '{pluginDll}'");
                        return new PluginPath(pluginDll);
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(packageType), packageType, null);
            }
        }

        public Task UninstallPluginAsync(PluginId pluginId, CancellationToken cancellationToken)
        {
            var installDirectory = GetInstallDirectory(pluginId);

            if (_fileSystemClient.DirectoryExists(installDirectory))
            {
                _logger.Warning($"Plugin '{pluginId}' is now installed, but request to be uninstalled");
                return Task.FromResult(0);
            }

            return Task.Run(() => _fileSystemClient.DeleteDirectory(installDirectory), cancellationToken);
        }

        public Task<IReadOnlyCollection<PluginPath>> GetInstalledPluginsAsync(CancellationToken cancellationToken)
        {
            var pluginInstallPath = _configuration.PluginInstallPath;

            var pluginPaths = _fileSystemClient.GetDirectories(pluginInstallPath)
                .Select(d => Path.Combine(d, $"{Path.GetFileName(d)}.dll"))
                .Where(_fileSystemClient.FileExists)
                .Select(p => PluginPath.With(p))
                .ToList();

            return Task.FromResult<IReadOnlyCollection<PluginPath>>(pluginPaths);
        }

        private string GetInstallDirectory(PluginId pluginId)
        {
            var pluginInstallPath = _configuration.PluginInstallPath;
            return Path.Combine(pluginInstallPath, pluginId.Value);
        }
    }
}