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
using System.Collections.Generic;
using Autofac;
using Serilog;
using TheBorg.Commands;
using TheBorg.Conversations;
using TheBorg.Plugins;
using TheBorg.Services;

namespace TheBorg
{
    public class TheBorgModule : Module
    {
        private static readonly ISet<Type> TypesNotRegisteredByConvention = new HashSet<Type>
            {
                typeof(Command),
                typeof(ActiveConversation),
                typeof(PluginProxy),
                typeof(PluginService)
            };

        private static readonly IReadOnlyCollection<Type> Singletons = new[]
            {
                typeof(PluginService),
            }; 

        protected override void Load(ContainerBuilder builder)
        {
            Serilog.Debugging.SelfLog.Out = Console.Out;

            var logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.ColoredConsole()
                .CreateLogger();

            builder.RegisterInstance(logger);
            builder
                .RegisterAssemblyTypes(typeof (TheBorgModule).Assembly)
                .Where(t => !TypesNotRegisteredByConvention.Contains(t))
                .AsImplementedInterfaces();

            foreach (var singleton in Singletons)
            {
                builder.RegisterType(singleton).AsImplementedInterfaces().SingleInstance();
            }
        }
    }
}