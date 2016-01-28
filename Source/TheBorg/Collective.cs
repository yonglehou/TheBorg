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
using System.Threading;
using System.Threading.Tasks;
using TheBorg.Clients;
using TheBorg.Clients.Slack;
using TheBorg.Commands;
using TheBorg.Tenants;
using TheBorg.Tenants.Slack;
using TheBorg.ValueObjects;

namespace TheBorg
{
    public class Collective : ICollective
    {
        private readonly ICommandManager _commandManager;
        private readonly ISlackTenant _slackTenant;

        public Collective(
            ICommandManager commandManager,
            ISlackTenant slackTenant)
        {
            _commandManager = commandManager;
            _slackTenant = slackTenant;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _slackTenant.ConnectAsync(cancellationToken).ConfigureAwait(false);
            _slackTenant.Messages.Subscribe(HandleMessage);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }

        private async void HandleMessage(TenantMessage tenantMessage)
        {
            // Yes, its async void

            await _commandManager.ExecuteAsync(tenantMessage, CancellationToken.None).ConfigureAwait(false);
        }
    }
}