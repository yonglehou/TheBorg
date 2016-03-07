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
using System.Text.RegularExpressions;

namespace TheBorg.Interface.Extensions
{
    public static class StringExtensions
    {
        private static readonly Regex RegexGroupRemover = new Regex(@"\(\?\<(?<group>[a-z0-9]+)\>.*?\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static string PrettyRegex(this string regex)
        {
            try
            {
                var prettyRegex = regex;

                prettyRegex = prettyRegex.Trim('^', '$');
                prettyRegex = RegexGroupRemover.Replace(prettyRegex, m => $"[{m.Groups["group"].Value.ToUpper()}]");

                return prettyRegex;
            }
            catch (Exception)
            {
                return regex;
            }
        }
    }
}