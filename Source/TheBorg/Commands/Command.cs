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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TheBorg.ValueObjects;

namespace TheBorg.Commands
{
    public class Command : ICommand
    {
        private readonly ICommandSet _commandSet;
        private readonly Regex _regex;
        private readonly MethodInfo _methodInfo;

        public Command(
            string regex,
            string help,
            ICommandSet commandSet,
            MethodInfo methodInfo)
        {
            Help = help;

            _commandSet = commandSet;
            _regex = new Regex(regex, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            _methodInfo = methodInfo;
        }

        public string Help { get; }

        public bool IsMatch(string text)
        {
            return _regex.IsMatch(text);
        }

        public Task ExecuteAsync(
            TenantMessage tenantMessage,
            CancellationToken cancellationToken,
            params object[] additionalArguments)
        {
            var match = _regex.Match(tenantMessage.Text);
            if (!match.Success)
            {
                throw new ArgumentException($"Text '{tenantMessage.Text}' was not a match");
            }

            var arguments = new List<object>();
            foreach (var parameterInfo in _methodInfo.GetParameters())
            {
                if (parameterInfo.ParameterType == typeof (CancellationToken))
                {
                    arguments.Add(cancellationToken);
                    continue;
                }
                if (parameterInfo.ParameterType == typeof (TenantMessage))
                {
                    arguments.Add(tenantMessage);
                    continue;
                }

                var additionalArgument = additionalArguments.FirstOrDefault(a => parameterInfo.ParameterType.IsInstanceOfType(a));
                if (additionalArgument != null)
                {
                    arguments.Add(additionalArgument);
                    continue;
                }
                
                arguments.Add(match.Groups[parameterInfo.Name].Value);
            }

            return (Task) _methodInfo.Invoke(_commandSet, arguments.ToArray());
        }
    }
}