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
using Nancy;
using TheBorg.Interface;

namespace TheBorg.Host
{
    public class ApiModule<T> : NancyModule
        where T : IHttpApi
    {
        public ApiModule(
            Func<IHttpApiContext, IHttpApi> httpApiFactory,
            IEnumerable<ApiEndpoint> apiEndpoints)
        {
            foreach (var apiEndpoint in apiEndpoints)
            {
                RouteBuilder routeBuilder;

                switch (apiEndpoint.Method)
                {
                    case Method.Get:
                        routeBuilder = Get;
                        break;
                    case Method.Post:
                        routeBuilder = Post;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                routeBuilder[apiEndpoint.Path, true] = (_, t) =>
                    {
                        var ss = new HttpApiContext();
                        return apiEndpoint.Invoker(ss, t, httpApiFactory(ss));
                    };
            }
        }
    }
}