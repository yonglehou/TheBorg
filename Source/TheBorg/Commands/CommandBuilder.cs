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

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Serilog;

namespace TheBorg.Commands
{
    public class CommandBuilder : ICommandBuilder
    {
        private readonly ILogger _logger;

        public CommandBuilder(
            ILogger logger)
        {
            _logger = logger;
        }

        public IReadOnlyCollection<ICommand> BuildCommands(IEnumerable<ICommandSet> commandSets)
        {
            var commands = new List<ICommand>();
            foreach (var commandSet in commandSets)
            {
                var commandSetType = commandSet.GetType();
                foreach (var methodInfo in commandSetType.GetMethods(BindingFlags.Instance | BindingFlags.Public))
                {
                    var commandAttribute = methodInfo.GetCustomAttributes<CommandAttribute>().SingleOrDefault();
                    if (commandAttribute == null) continue;

                    _logger.Debug($"Adding command '{commandSetType.Name}.{methodInfo.Name}': {commandAttribute.Help}");

                    commands.Add(new Command(
                        commandAttribute.Regex,
                        commandAttribute.Help,
                        commandSet,
                        methodInfo));
                }
            }

            return commands;
        }
    }
}