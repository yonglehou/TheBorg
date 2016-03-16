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
using System.Threading;
using System.Threading.Tasks;
using TheBorg.Common.Tenants;
using TheBorg.Interface.ValueObjects;
using TheBorg.Interface.ValueObjects.Tenants;

namespace TheBorg.Collective.Services
{
    public class MessageService : IMessageService
    {
        private readonly IReadOnlyDictionary<TenantKey, ITenant> _tenants;

        public MessageService(
            IEnumerable<ITenant> tenants)
        {
            _tenants = tenants.ToDictionary(t => t.TenantKey, t => t);
        }

        public Task ReplyAsync(TenantMessage tenantMessage, string text, CancellationToken cancellationToken)
        {
            return SendAsync(tenantMessage.Address, text, cancellationToken);
        }

        public Task SendAsync(Address address, string text, CancellationToken cancellationToken)
        {
            ITenant tenant;
            if (!_tenants.TryGetValue(address.TenantKey, out tenant))
            {
                throw new ArgumentException($"There's no tenant with key '{address.TenantKey}'");
            }

            return tenant.SendMessage(address, text, cancellationToken);
        }
    }
}