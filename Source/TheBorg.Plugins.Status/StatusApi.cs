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

using System.Threading;
using System.Threading.Tasks;
using TheBorg.Interface;
using TheBorg.Interface.Apis;
using TheBorg.Interface.Attributes;
using TheBorg.Interface.ValueObjects.Settings;
using TheBorg.Interface.ValueObjects.Tenants;

namespace TheBorg.Plugins.Status
{
    public class StatusApi : IPluginHttpApi
    {
        private readonly ISettingApi _settingApi;
        private readonly IMessageApi _messageApi;

        public StatusApi(
            ISettingApi settingApi,
            IMessageApi messageApi)
        {
            _settingApi = settingApi;
            _messageApi = messageApi;
        }

        [HttpApi(HttpApiMethod.Post, "api/commands/ping")]
        [Command("^ping (?<message>.+)$", "pings the borg")]
        public async Task Ping([FromBody]TenantMessage tenantMessage, CancellationToken cancellationToken)
        {
            await _settingApi.SetAsync(SettingKey.With("last-ping-message"), tenantMessage.Text, cancellationToken).ConfigureAwait(false);

            var reply = tenantMessage.CreateReply(
                "pong",
                TenantMessageAttachmentProperty.With("text", tenantMessage.Text),
                TenantMessageAttachmentProperty.With("color", "#333333"));

            await _messageApi.SendAsync(reply, cancellationToken).ConfigureAwait(false);
        }
    }
}