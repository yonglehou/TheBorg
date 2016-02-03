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

using System;
using System.Collections.Generic;
using System.Configuration;

namespace TheBorg.Services
{
    public class ConfigurationService : IConfigurationService
    {
        private static readonly IReadOnlyDictionary<Type, Func<string, object>> Converters = new Dictionary<Type, Func<string, object>>
            {
                {typeof(string), s => s},
                {typeof(int), s => int.Parse(s)},
                {typeof(bool), s => bool.Parse(s)},
                {typeof(Uri), s => new Uri(s, UriKind.Absolute)},
            };

        public T Get<T>(string key, T defaultValue)
        {
            T value;
            return TryGet(key, out value)
                ? value
                : defaultValue;
        }

        public T Get<T>(string key)
        {
            T value;
            if (!TryGet(key, out value))
            {
                throw new ConfigurationErrorsException(
                     $"No configuration present for key '{key}'");
            }
            return value;
        }

        public bool TryGet<T>(string key, out T value)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            var configValue = ConfigurationManager.AppSettings[key];
            if (string.IsNullOrEmpty(configValue))
            {
                value = default(T);
                return false;
            }

            Func<string, object> converter;
            if (!Converters.TryGetValue(typeof (T), out converter))
            {
                throw new ConfigurationErrorsException(
                    $"Dont know how to parse config key '{key}' to type '{typeof(T).Name}'");
            }

            try
            {
                value = (T) converter(configValue);
                return true;
            }
            catch (Exception e)
            {
                throw new ConfigurationErrorsException(
                    $"Failed to parse config key '{key}' with value '{configValue}' to type '{typeof(T).Name}': {e.Message}",
                    e);
            }
        }
    }
}