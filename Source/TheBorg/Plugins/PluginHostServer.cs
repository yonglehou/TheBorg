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

using Serilog;
using TheBorg.Interface;
using TheBorg.Interface.ValueObjects;
using TheBorg.Services;

namespace TheBorg.Plugins
{
    public class PluginHostServer : IPluginHost
    {
        private readonly ILogger _logger;
        private readonly IMessageService _messageService;

        public PluginHostServer(
            ILogger logger,
            IMessageService messageService)
        {
            _logger = logger;
            _messageService = messageService;
        }

        public void Log(LogMessage logMessage)
        {
            _logger.Debug(logMessage.Text);
        }

        public async void Send(Address address, string text)
        {
            await _messageService.SendAsync(address, text).ConfigureAwait(false);
        }

        public async void Reply(TenantMessage tenantMessage, string text)
        {
            await _messageService.ReplyAsync(tenantMessage, text).ConfigureAwait(false);
        }
    }
}