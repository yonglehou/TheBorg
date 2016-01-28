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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using TheBorg.Clients.Slack;
using TheBorg.Tenants.Slack;
using TheBorg.ValueObjects;

namespace TheBorg.Commands
{
    public class CommandManager : ICommandManager
    {
        private readonly ILogger _logger;
        private readonly IReadOnlyCollection<ICommand> _commands;

        public CommandManager(
            ILogger logger,
            ICommandBuilder commandBuilder,
            IEnumerable<ICommandSet> commandSets)
        {
            _logger = logger;
            _commands = commandBuilder.BuildCommands(commandSets);
        }

        public async Task ExecuteAsync(TenantMessage tenantMessage, CancellationToken cancellationToken)
        {
            var commandThatUnderstandMessage = _commands
                .Where(c => c.IsMatch(tenantMessage.Text))
                .ToList();
            if (!commandThatUnderstandMessage.Any())
            {
                _logger.Information($"Did not find any commands that understand: {tenantMessage.Text}");
                return;
            }
            if (commandThatUnderstandMessage.Count != 1)
            {
                _logger.Warning($"Foung too many commands that understand: {tenantMessage.Text}");
                return;
            }

            var command = commandThatUnderstandMessage.Single();

            await command.ExecuteAsync(tenantMessage, cancellationToken).ConfigureAwait(false);
        }
    }
}