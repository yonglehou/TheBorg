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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nancy.IO;
using Newtonsoft.Json;
using TheBorg.Interface;

namespace TheBorg.Host
{
    public class HttpApiRequestContext : IHttpApiRequestContext
    {
        private readonly RequestStream _body;
        private readonly IReadOnlyDictionary<string, object> _parameters;

        public HttpApiRequestContext(
            RequestStream body,
            IEnumerable<KeyValuePair<string, object>> parameters)
        {
            _body = body;
            _parameters = parameters.ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        public T Get<T>(string key)
        {
            object value;
            if (_parameters.TryGetValue(key, out value))
            {
                return (T) value;
            }

            throw new ArgumentOutOfRangeException(nameof(key), $"There's no parameter named '{key}'");
        }

        public object BodyAs(Type type)
        {
            using (var streamReader = new StreamReader(_body))
            {
                // TODO: Make async

                var content = streamReader.ReadToEnd();
                return JsonConvert.DeserializeObject(content, type);
            }
        }
    }
}