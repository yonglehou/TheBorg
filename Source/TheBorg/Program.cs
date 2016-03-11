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

using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Autofac;
using TheBorg.Collective;
using TheBorg.Collective.PluginManagement;
using TheBorg.Common;
using TheBorg.Plugins.Administration;
using TheBorg.Plugins.GitHub;
using TheBorg.Plugins.Help;
using TheBorg.Plugins.Jokes;
using TheBorg.Plugins.Status;
using TheBorg.Tenants.Slack;
using Topshelf;

namespace TheBorg
{
    internal class Program
    {
        private static readonly IContainer Container;

        private static readonly IReadOnlyCollection<Assembly> BuiltInPlugins = new[]
        {
            typeof (JokesPluginBootstrapper).Assembly,
            typeof (StatusPluginBootstrapper).Assembly,
            typeof (PluginsPluginBootstrapper).Assembly,
            typeof (HelpPluginBootstrapper).Assembly,
            typeof (GitHubPluginBootstrapper).Assembly
        };

        static Program()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterModule<TheBorgCoreModule>();
            containerBuilder.RegisterModule<TheBorgTenantsSlack>();
            containerBuilder.RegisterModule<TheBorgCollectivePluginManagement>();
            containerBuilder.RegisterModule<TheBorgCollective>();
            Container = containerBuilder.Build();
        }

        private static int Main()
        {
            return (int) HostFactory.Run(hc =>
            {
                hc.Service<IBorg>(s =>
                {
                    s.ConstructUsing(() => Container.Resolve<IBorg>());
                    s.WhenStarted(c => { c.StartAsync(BuiltInPlugins, CancellationToken.None).Wait(); });
                    s.WhenStopped(c =>
                    {
                        c.StopAsync(CancellationToken.None).Wait();
                        Container.Dispose();
                    });
                });

                hc.DependsOnMsSql();
                hc.RunAsLocalSystem();
                hc.SetDisplayName("TheBorg");
                hc.SetServiceName("TheBorg");
                hc.SetDescription($"The Borg v{typeof (Program).Assembly.GetName().Version}");
            });
        }
    }
}