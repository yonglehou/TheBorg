﻿// The MIT License(MIT)
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
using System.Threading;
using Microsoft.CSharp.RuntimeBinder;
using Nancy;
using TheBorg.Interface;
using TheBorg.Interface.Apis;
using TheBorg.Interface.Attributes;
using TheBorg.Interface.ValueObjects;
using TheBorg.Interface.ValueObjects.Plugins;

namespace TheBorg.Host
{
    public class PluginRegistration : IPluginRegistration
    {
        public ICommandApi CommandApi { get; }
        public IConfigApi ConfigApi { get; }
        public IMessageApi MessageApi { get; }
        public IHttpApi HttpApi { get; }
        public ISettingApi SettingApi { get; }
        public IPluginApi PluginApi { get; }
        public Uri Uri { get; }
        public Token Token { get; }

        private readonly List<CommandDescription> _commandDescriptions = new List<CommandDescription>();
        private readonly Dictionary<Type, Func<IHttpApiRequestContext, INancyModule>> _factories = new Dictionary<Type, Func<IHttpApiRequestContext, INancyModule>>();

        public PluginInformation PluginInformation { get; private set; }
        public IReadOnlyCollection<CommandDescription> CommandDescriptions => _commandDescriptions;

        public PluginRegistration(
            Uri uri,
            Token token,
            ICommandApi commandApi,
            IMessageApi messageApi,
            IHttpApi httpApi,
            IPluginApi pluginApi,
            IConfigApi configApi,
            ISettingApi settingApi)
        {
            Uri = uri;
            Token = token;
            CommandApi = commandApi;
            MessageApi = messageApi;
            HttpApi = httpApi;
            PluginApi = pluginApi;
            ConfigApi = configApi;
            SettingApi = settingApi;
        }

        public IPluginRegistration SetPluginInformation(PluginInformation pluginInformation)
        {
            PluginInformation = pluginInformation;
            return this;
        }

        public IPluginRegistration RegisterCommands(params CommandDescription[] commandDescriptions)
        {
            return RegisterCommands((IEnumerable<CommandDescription>) commandDescriptions);
        }

        public IPluginRegistration RegisterCommands(IEnumerable<CommandDescription> commandDescriptions)
        {
            _commandDescriptions.AddRange(commandDescriptions);
            return this;
        }

        public IPluginRegistration RegisterHttpApiCommands()
        {
            foreach (var a in _factories.Keys
                .Select(t => t.GetGenericArguments()[0])
                .SelectMany(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Public).Select(mi => new { Type = t, Method = mi })))
            {
                var commandAttribute = a.Method.GetCustomAttributes<CommandAttribute>().SingleOrDefault();
                var httpApiAttribute = a.Method.GetCustomAttributes<HttpApiAttribute>().SingleOrDefault();
                if (commandAttribute == null || httpApiAttribute == null)
                {
                    continue;
                }

                _commandDescriptions.Add(new CommandDescription(
                    commandAttribute.Regex,
                    commandAttribute.Help,
                    httpApiAttribute.Path));
            }

            return this;
        }

        public IPluginRegistration RegisterHttpApi<T>(T instance) where T : IPluginHttpApi
        {
            return RegisterHttpApi(_ => instance);
        }

        public IPluginRegistration RegisterHttpApi<T>(Func<IHttpApiRequestContext, T> factory)
            where T : IPluginHttpApi
        {
            var apiEndpoints = GatherEndpoints<T>();
            _factories.Add(typeof(ApiModule<T>), c => new ApiModule<T>(factory, apiEndpoints));
            return this;
        }

        public IEnumerable<KeyValuePair<Type, Func<IHttpApiRequestContext, INancyModule>>> GetModules()
        {
            return _factories;
        }

        private static IEnumerable<ApiEndpoint> GatherEndpoints<T>()
            where T : IPluginHttpApi
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
                httpApiAttribute.HttpApiMethod,
                httpApiAttribute.Path,
                async (context, token, api) =>
                    {
                        var parameters = BuildParameters(methodInfo, context, token).ToArray();
                        dynamic task = methodInfo.Invoke(api, parameters);

                        try
                        {
                            return await task.ConfigureAwait(false);
                        }
                        catch (RuntimeBinderException e) when (e.Message.Contains("'void'"))
                        {
                            // TODO: Fix this as its ugly as hell!
                            return null as dynamic;
                        }
                    });
        }

        private static IEnumerable<object> BuildParameters(
            MethodInfo methodInfo,
            IHttpApiRequestContext httpApiRequestContext,
            CancellationToken cancellationToken)
        {
            return methodInfo
                .GetParameters()
                .Select(pi =>
                    {
                        if (pi.ParameterType == typeof (CancellationToken))
                        {
                            return (object) cancellationToken;
                        }
                        if (pi.ParameterType == typeof (IHttpApiRequestContext))
                        {
                            return (object) httpApiRequestContext;
                        }
                        if (pi.GetCustomAttribute<FromBodyAttribute>() != null)
                        {
                            return httpApiRequestContext.BodyAs(pi.ParameterType);
                        }

                        throw new InvalidOperationException($"Unknown type '{pi.ParameterType}'");
                    });
        }
    }
}