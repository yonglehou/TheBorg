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
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Autofac;
using Autofac.Integration.WebApi;
using Microsoft.Owin.Hosting;
using Owin;
using TheBorg.PluginManagement.HttpApi.Middlewares;

namespace TheBorg.PluginManagement.HttpApi
{
    public class PluginHttpApi : IPluginHttpApi
    {
        private readonly ILifetimeScope _lifetimeScope;
        private IDisposable _webApp;

        public PluginHttpApi(
            ILifetimeScope lifetimeScope)
        {
            _lifetimeScope = lifetimeScope;
        }

        public Task StartAsync(int port, CancellationToken cancellationToken)
        {
            _webApp = WebApp.Start($"http://127.0.0.1:{port}", Configuration);
            return Task.FromResult(0);
        }

        public void Configuration(IAppBuilder app)
        {
            var httpConfiguration = new HttpConfiguration
                {
                    DependencyResolver = new AutofacWebApiDependencyResolver(_lifetimeScope)
                };
            httpConfiguration.MapHttpAttributeRoutes();

            app.Use<PluginAuthMiddleware>();
            app.UseAutofacMiddleware(_lifetimeScope);
            app.UseAutofacWebApi(httpConfiguration);
            app.UseWebApi(httpConfiguration);
        }

        public void Dispose()
        {
            _webApp.Dispose();
        }
    }
}