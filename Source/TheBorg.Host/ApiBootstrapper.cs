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
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Responses.Negotiation;
using Nancy.TinyIoc;
using TheBorg.Interface;

namespace TheBorg.Host
{
    internal class ApiBootstrapper : DefaultNancyBootstrapper
    {
        private readonly ApiCatalog _apiCatalog;

        public ApiBootstrapper(
            IEnumerable<KeyValuePair<Type, Func<IHttpApiRequestContext, INancyModule>>> nancyModules)
        {
            _apiCatalog = new ApiCatalog(nancyModules);
        }

        protected override void ConfigureApplicationContainer(TinyIoCContainer container)
        {
            container.Register<INancyModuleCatalog>(_apiCatalog);
        }

        protected override NancyInternalConfiguration InternalConfiguration
        {
            get
            {
                var processors = new[] { typeof(JsonProcessor), };
                return NancyInternalConfiguration.WithOverrides(x => x.ResponseProcessors = processors);
            }
        }

        private class ApiCatalog : INancyModuleCatalog
        {
            private readonly IReadOnlyDictionary<Type, Func<IHttpApiRequestContext, INancyModule>> _nancyModules;

            public ApiCatalog(
                IEnumerable<KeyValuePair<Type, Func<IHttpApiRequestContext, INancyModule>>> nancyModules)
            {
                _nancyModules = nancyModules.ToDictionary(kv => kv.Key, kv => kv.Value);
            }

            public IEnumerable<INancyModule> GetAllModules(NancyContext context)
            {
                var apiContext = new HttpApiRequestContext(context?.Request?.Body, Enumerable.Empty<KeyValuePair<string, object>>());
                return _nancyModules.Values.Select(f => f(apiContext));
            }

            public INancyModule GetModule(Type moduleType, NancyContext context)
            {
                Func<IHttpApiRequestContext, INancyModule> nancyModule;
                if (_nancyModules.TryGetValue(moduleType, out nancyModule))
                {
                    var apiContext = new HttpApiRequestContext(context?.Request?.Body, Enumerable.Empty<KeyValuePair<string, object>>());
                    return nancyModule(apiContext);
                }

                throw new Exception($"Module with type '{moduleType}' not found");
            }
        }
    }
}