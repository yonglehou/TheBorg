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
using TheBorg.Interface;
using TheBorg.Interface.ValueObjects.Plugins;

namespace TheBorg.Plugins.Help
{
    public class HelpPluginBootstrapper : IPluginBootstrapper
    {
        public void Start(Action<Action<IPluginRegistration>> pluginRegistra)
        {
            pluginRegistra(r =>
                {
                    var assembly = typeof(HelpPluginBootstrapper).Assembly;

                    r.SetPluginInformation(PluginInformation.With(
                        PluginId.From(assembly),
                        PluginTitle.With("Help"),
                        PluginVersion.From(assembly),
                        PluginDescription.With("Provides help for the Borg"),
                        r.Uri));
                    r.RegisterHttpApi(new HelpApi(r.HttpApi, r.MessageApi, r.PluginApi));
                    r.RegisterHttpApiCommands();
                });
        }
    }
}