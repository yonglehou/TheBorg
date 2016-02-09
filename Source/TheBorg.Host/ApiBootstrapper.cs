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
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.TinyIoc;
using TheBorg.Interface;

namespace TheBorg.Host
{
    public class ApiBootstrapper : DefaultNancyBootstrapper
    {
        private readonly ApiCatalog _apiCatalog;

        public ApiBootstrapper(
            IEnumerable<KeyValuePair<Type, Func<IHttpApiContext, INancyModule>>> nancyModules)
        {
            _apiCatalog = new ApiCatalog(nancyModules);
        }

        protected override void ConfigureApplicationContainer(TinyIoCContainer container)
        {
            container.Register<INancyModuleCatalog>(_apiCatalog);
        }

        private class ApiCatalog : INancyModuleCatalog
        {
            private readonly IReadOnlyDictionary<Type, Func<IHttpApiContext, INancyModule>> _nancyModules;

            public ApiCatalog(
                IEnumerable<KeyValuePair<Type, Func<IHttpApiContext, INancyModule>>> nancyModules)
            {
                _nancyModules = nancyModules.ToDictionary(kv => kv.Key, kv => kv.Value);
            }

            public IEnumerable<INancyModule> GetAllModules(NancyContext context)
            {
                var apiContext = new HttpApiContext();
                return _nancyModules.Values.Select(f => f(apiContext));
            }

            public INancyModule GetModule(Type moduleType, NancyContext context)
            {
                Func<IHttpApiContext, INancyModule> nancyModule;
                if (_nancyModules.TryGetValue(moduleType, out nancyModule))
                {
                    var apiContext = new HttpApiContext();
                    return nancyModule(apiContext);
                }

                throw new Exception($"Module with type '{moduleType}' not found");
            }
        }
    }
}