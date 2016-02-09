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
using System.Reflection;
using Nancy;
using TheBorg.Interface;

namespace TheBorg.Host
{
    public class PluginRegistration : IPluginRegistration
    {
        private readonly Dictionary<Type, Func<IHttpApiContext, INancyModule>> _factories = new Dictionary<Type, Func<IHttpApiContext, INancyModule>>(); 

        public IPluginRegistration RegisterApi<T>(T instance) where T : IHttpApi
        {
            return RegisterApi(_ => instance);
        }

        public IPluginRegistration RegisterApi<T>(Func<IHttpApiContext, T> factory)
            where T : IHttpApi
        {
            var apiEndpoints = GatherEndpoints<T>();
            _factories.Add(typeof(T), c => new ApiModule<T>(hc => factory(hc), apiEndpoints)); // TODO: Check for false!
            return this;
        }

        public IEnumerable<KeyValuePair<Type, Func<IHttpApiContext, INancyModule>>> GetModules()
        {
            return _factories;
        }

        private static IEnumerable<ApiEndpoint> GatherEndpoints<T>()
            where T : IHttpApi
        {
            return typeof(T)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Select(mi => new
                    {
                        HttpApi = mi.GetCustomAttribute<HttpApiAttribute>(),
                        MethodInfo = mi,
                    })
                .Where(a => a.HttpApi != null)
                .Select(a => CreateApiEndpoint(a.MethodInfo, a.HttpApi));
        }

        private static ApiEndpoint CreateApiEndpoint(MethodInfo methodInfo, HttpApiAttribute httpApiAttribute)
        {
            // TODO: Build argument list

            return new ApiEndpoint(
                httpApiAttribute.HttpMethod,
                httpApiAttribute.Path,
                async (context, token, api) =>
                    {
                        dynamic task = methodInfo.Invoke(api, new object[] {});
                        return await task.ConfigureAwait(false);
                    });
        }
    }
}