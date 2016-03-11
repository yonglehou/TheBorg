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

using System.Net;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace TheBorg.Collective.PluginManagement.HttpApi.Middlewares
{
    public class PluginAuthMiddleware : OwinMiddleware
    {
        private class PluginPrincipal : IPrincipal
        {
            public PluginPrincipal(IIdentity identity)
            {
                Identity = identity;
            }

            public bool IsInRole(string role)
            {
                return false;
            }

            public IIdentity Identity { get; }
        }

        private class PluginIdentity : IIdentity
        {
            public PluginIdentity(string name)
            {
                Name = name;
                AuthenticationType = "plugin";
                IsAuthenticated = true;
            }

            public string Name { get; }
            public string AuthenticationType { get; }
            public bool IsAuthenticated { get; }
        }

        public PluginAuthMiddleware(OwinMiddleware next) : base(next)
        {
        }

        public override async Task Invoke(IOwinContext context)
        {
            if (!context.Request.Headers.ContainsKey("Authorization"))
            {
                context.Response.StatusCode = (int) HttpStatusCode.Forbidden;
                return;
            }

            var headerParts = context.Request.Headers.Get("Authorization").Split(' ');

            context.Request.User = new PluginPrincipal(new PluginIdentity(headerParts[1]));

            await Next.Invoke(context).ConfigureAwait(false);
        }
    }
}