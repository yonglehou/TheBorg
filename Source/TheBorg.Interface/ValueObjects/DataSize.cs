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

namespace TheBorg.Interface.ValueObjects
{
    public class DataSize : SingleValueObject<long>
    {
        private readonly Lazy<string> _toStringLazy;

        public DataSize(long value) : base(value)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), $"Data size must be positive '{value}'");

            _toStringLazy = new Lazy<string>(() => PrettyPrint(value));
        }

        public static DataSize With(long value)
        {
            return new DataSize(value);
        }

        public override string ToString()
        {
            return _toStringLazy.Value;
        }

        private static string PrettyPrint(long value)
        {
            const int byteConversion = 1024;
            var bytes = Convert.ToDouble(value);

            if (bytes >= Math.Pow(byteConversion, 3))
            {
                return string.Concat(Math.Round(bytes/Math.Pow(byteConversion, 3), 2), " GB");
            }
            if (bytes >= Math.Pow(byteConversion, 2))
            {
                return string.Concat(Math.Round(bytes/Math.Pow(byteConversion, 2), 2), " MB");
            }
            if (bytes >= byteConversion)
            {
                return string.Concat(Math.Round(bytes/byteConversion, 2), " KB");
            }
            return string.Concat(bytes, " B");
        }
    }
}