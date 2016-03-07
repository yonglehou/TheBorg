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

using System.Threading;
using Autofac;
using TheBorg.Core;
using TheBorg.Host;
using TheBorg.PluginManagement;
using Topshelf;

namespace TheBorg
{
    internal class Program
    {
        private static readonly IContainer Container;

        static Program()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterModule<TheBorgCoreModule>();
            containerBuilder.RegisterModule<TheBorgPluginManagementModule>();
            containerBuilder.RegisterModule<TheBorgModule>();
            Container = containerBuilder.Build();
        }

        static int Main(string[] args)
        {
            return (int) HostFactory.Run(hc =>
            {
                hc.Service<ICollective>(s =>
                {
                    s.ConstructUsing(() => Container.Resolve<ICollective>());
                    s.WhenStarted(c =>
                    {
                        using (var a = AsyncHelper.Wait)
                        {
                            a.Run(c.StartAsync(CancellationToken.None));
                        }
                    });
                    s.WhenStopped(c =>
                    {
                        using (var a = AsyncHelper.Wait)
                        {
                            a.Run(c.StopAsync(CancellationToken.None));
                        }
                        Container.Dispose();
                    });
                });

                hc.RunAsLocalSystem();
                hc.SetDisplayName("TheBorg");
                hc.SetServiceName("TheBorg");
                hc.SetDescription($"The Borg v{typeof(Program).Assembly.GetName().Version}");
            });
        }
    }
}