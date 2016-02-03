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
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Halibut;
using Halibut.ServiceModel;
using TheBorg.Interface;
using TheBorg.Interface.ValueObjects;

namespace TheBorg.Host
{
    public class PluginHostClient : MarshalByRefObject, IPluginHost
    {
        private HalibutRuntime _halibutRuntimeClient;
        private HalibutRuntime _halibutRuntimeServer;
        private IPluginHost _pluginHost;

        public void Launch(string pluginPath, AppDomain appDomain, byte[] x509CertificateBytes, string serverThumbprint, int serverPort, int clientPort)
        {
            var x509Certificate2 = new X509Certificate2(x509CertificateBytes);
            _halibutRuntimeClient = new HalibutRuntime(x509Certificate2);
            _halibutRuntimeClient.Trust(serverThumbprint);
            _pluginHost = _halibutRuntimeClient.CreateClient<IPluginHost>($"https://127.0.0.1:{serverPort}", serverThumbprint);

            var assembly = appDomain.Load(AssemblyName.GetAssemblyName(pluginPath));
            var pluginDirectory = Path.GetDirectoryName(pluginPath);
            appDomain.AssemblyResolve += (sender, args) =>
                {
                    var ad = sender as AppDomain;
                    var path = Path.Combine(pluginDirectory, args.Name.Split(',')[0] + ".dll");
                    return ad.Load(path);
                };
            var pluginType = assembly.GetTypes().Single(t => typeof (IPlugin).IsAssignableFrom(t));
            var plugin = (IPlugin) Activator.CreateInstance(pluginType);

            plugin.Launch(this);

            var delegateServiceFactory = new DelegateServiceFactory();
            delegateServiceFactory.Register(() => plugin);

            _halibutRuntimeServer = new HalibutRuntime(delegateServiceFactory, x509Certificate2);
            _halibutRuntimeServer.Trust(serverThumbprint);
            _halibutRuntimeServer.Listen(new IPEndPoint(IPAddress.Loopback, clientPort));
        }

        public void Log(LogMessage logMessage)
        {
            _pluginHost.Log(logMessage);
        }

        public void Send(Address address, string text)
        {
            _pluginHost.Send(address, text);
        }

        public void Reply(TenantMessage tenantMessage, string text)
        {
            _pluginHost.Reply(tenantMessage, text);
        }
    }
}