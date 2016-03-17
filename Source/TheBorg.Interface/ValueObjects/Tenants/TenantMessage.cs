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

namespace TheBorg.Interface.ValueObjects.Tenants
{
    public class TenantMessage
    {
        public static TenantMessage To(
            Address address,
            string text,
            params TenantMessageAttachment[] tenantMessageAttachments)
        {
            return new TenantMessage(
                text,
                address,
                tenantMessageAttachments);
        }

        public TenantMessage(
            string text,
            Address address,
            IEnumerable<TenantMessageAttachment> attachments)
        {
            if (string.IsNullOrEmpty(text)) throw new ArgumentNullException(nameof(text));
            if (address == null) throw new ArgumentNullException(nameof(address));

            Text = text;
            Address = address;
            Attachments = (attachments ?? Enumerable.Empty<TenantMessageAttachment>()).ToList();
        }

        public string Text { get; }
        public Address Address { get; }
        public IReadOnlyCollection<TenantMessageAttachment> Attachments { get; set; }

        public TenantMessage CreateReply(string text, params TenantMessageAttachmentProperty[] tenantMessageAttachmentsProperties)
        {
            var tenantMessageAttachments = tenantMessageAttachmentsProperties.Any()
                ? new[] {new TenantMessageAttachment(tenantMessageAttachmentsProperties)}
                : Enumerable.Empty<TenantMessageAttachment>();

            return new TenantMessage(
                text,
                Address,
                tenantMessageAttachments);
        }
    }
}