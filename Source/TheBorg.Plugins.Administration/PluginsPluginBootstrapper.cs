﻿// The MIT License(MIT)
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
using System.Collections.Generic;
using TheBorg.Interface;
using TheBorg.Interface.ValueObjects;
using TheBorg.Interface.ValueObjects.Plugins;

namespace TheBorg.Plugins.Administration
{
    public class PluginsPluginBootstrapper : IPluginBootstrapper
    {
        public void Start(Action<Action<IPluginRegistration>> pluginRegistra)
        {
            pluginRegistra(r =>
                {
                    var assembly = typeof(PluginsPluginBootstrapper).Assembly;

                    r.SetPluginInformation(new PluginInformation(
                        PluginId.From(assembly),
                        PluginTitle.With("Admin"),
                        PluginVersion.From(assembly),
                        PluginDescription.With("Provides administration"),
                        r.Uri));
                    r.RegisterHttpApi(new PluginsApi(r.PluginApi, r.MessageApi));
                    r.RegisterCommands(
                        CommandDescriptions());
                });
        }

        private static IEnumerable<CommandDescription> CommandDescriptions()
        {
            yield return new CommandDescription("^plugins$", "list all plugins", "api/commands/list-plugins");
            yield return new CommandDescription(@"^unload plugin (?<pluginId>[a-z0-9\.]+)$", "unload specific plugin", "api/commands/unload-plugin");
            yield return new CommandDescription("^install plugin (?<url>.+)$", "installs plugin", "api/commands/install-plugin");
        }
    }
}