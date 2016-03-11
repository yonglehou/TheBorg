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
using System.Linq;
using System.Reflection;
using Autofac;
using Module = Autofac.Module;

namespace TheBorg.Common
{
    public abstract class ConventionModule : Module
    {
        protected Assembly Assembly => GetType().Assembly;

        protected override void Load(ContainerBuilder builder)
        {
            var typesToSkip = new HashSet<Type>(Enumerable.Empty<Type>()
                .Concat(TypesToSkip())
                .Concat(SingletonTypes()));

            builder
                .RegisterAssemblyTypes(GetType().Assembly)
                .Where(t => !typesToSkip.Contains(t))
                .Where(t => BaseTypesToSkip().All(b => !b.IsAssignableFrom(t)) )
                .AsImplementedInterfaces()
                .OwnedByLifetimeScope();

            foreach (var singleton in SingletonTypes())
            {
                builder.RegisterType(singleton).AsImplementedInterfaces().SingleInstance();
            }
        }

        protected virtual IEnumerable<Type> TypesToSkip()
        {
            yield break;
        }

        protected virtual IEnumerable<Type> BaseTypesToSkip()
        {
            yield break;
        }

        protected virtual IEnumerable<Type> SingletonTypes()
        {
            yield break;
        } 
    }
}