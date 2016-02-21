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
using System.Linq;
using System.Reflection;
using System.Threading;
using TheBorg.Host.Apis;
using TheBorg.Interface;
using TheBorg.PluginManagement.ValueObjects;

namespace TheBorg.Host
{
    public class PluginHostClient : MarshalByRefObject
    {
        private ApiHost _apiHost;

        public void Launch(string pluginPath, AppDomain appDomain, Uri pluginApiUri, int clientPort)
        {
            var pluginId = new PluginPath(pluginPath).GetPluginId();
            var assembly = appDomain.Load(AssemblyName.GetAssemblyName(pluginPath));
            var pluginDirectory = Path.GetDirectoryName(pluginPath);
            appDomain.AssemblyResolve += (sender, args) =>
                {
                    var ad = sender as AppDomain;
                    var path = Path.Combine(pluginDirectory, args.Name.Split(',')[0] + ".dll");
                    return ad.Load(path);
                };
            var pluginBootstrapperType = assembly.GetTypes().Single(t => typeof (IPluginBootstrapper).IsAssignableFrom(t));
            var pluginBootstrapper = (IPluginBootstrapper) Activator.CreateInstance(pluginBootstrapperType);

            var pluginRegistration = new PluginRegistration(
                new CommandApi(pluginId, pluginApiUri),
                new MessageApi(pluginId, pluginApiUri));

            pluginBootstrapper.Start(r =>
                {
                    r(pluginRegistration);
                    if (pluginRegistration.PluginInformation == null)
                    {
                        throw new InvalidOperationException("You must call SetPluginInformation(...)");
                    }

                    using (var a = AsyncHelper.Wait)
                    {
                        a.Run(pluginRegistration.CommandApi.RegisterAsync(
                            pluginRegistration.CommandDescriptions,
                            CancellationToken.None));
                    }
                });
            
            _apiHost = new ApiHost();
            _apiHost.Start(clientPort, pluginRegistration);
        }
    }
}