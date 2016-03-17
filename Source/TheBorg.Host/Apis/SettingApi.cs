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

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheBorg.Interface.Apis;
using TheBorg.Interface.ValueObjects;
using TheBorg.Interface.ValueObjects.Settings;

namespace TheBorg.Host.Apis
{
    public class SettingApi : Api, ISettingApi
    {
        public SettingApi(
            Uri baseUri,
            Token token)
            : base(baseUri, token)
        {
        }

        public Task<string> GetAsync(SettingKey settingKey, CancellationToken cancellationToken)
        {
            return GetAsync(
                $"/api/settings/{settingKey.Value}",
                cancellationToken);
        }

        public Task SetAsync(SettingKey settingKey, string value, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value));

            return PostAsync(
                $"/api/settings/{settingKey.Value}",
                value,
                cancellationToken);
        }

        public Task<IReadOnlyCollection<SettingKey>> GetKeysAsync(CancellationToken cancellationToken)
        {
            return GetAsAsync<IReadOnlyCollection<SettingKey>>(
                "/api/settings",
                cancellationToken);
        }
    }
}