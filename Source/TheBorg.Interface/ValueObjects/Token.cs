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
using TheBorg.Interface.ValueObjects.Plugins;

namespace TheBorg.Interface.ValueObjects
{
    public class Token : SingleValueObject<string>
    {
        public static Token With(string value) { return new Token(value); }

        public static Token NewFor(PluginId pluginId)
        {
            return With($"{Guid.NewGuid().ToString("D")}@{pluginId.Value}");
        }

        public Token(string value) : base(value)
        {
            if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value));

            var parts = value.Split('@');
            if (parts.Length != 2) throw new ArgumentException($"Malformed token '{value}' (parts)", nameof(value));

            Guid tId;
            if (!Guid.TryParse(parts[0], out tId)) throw new ArgumentException($"Malformed token '{value}' (guid)", nameof(value));

            var pluginId = PluginId.With(parts[1]);

            TokenId = TokenId.With(tId);
            PluginId = pluginId;
        }

        public TokenId TokenId { get; }
        public PluginId PluginId { get; }
    }
}