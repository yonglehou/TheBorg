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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheBorg.Collective.MsSql;

namespace TheBorg.Collective.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly IMsSqlConnection _msSqlConnection;
        private const string SetSql = @"
            UPDATE [dbo].[Settings] SET [Value] = @Value WHERE [Key] = @Key;
            IF @@ROWCOUNT = 0
            BEGIN
                INSERT [dbo].[Settings] ([Key], [Value]) VALUES (@Key, @Value);
            END";
        private const string GetSql = "SELECT [Value] FROM [dbo].[Settings] WHERE [Key] = @Key";
        private const string DeleteSql = "DELETE FROM [dbo].[Settings] WHERE [Key] = @Key";

        public SettingsService(
            IMsSqlConnection msSqlConnection)
        {
            _msSqlConnection = msSqlConnection;
        }

        public async Task<string> GetAsync(string key, CancellationToken cancellationToken)
        {
            ValidateKey(key);

            var values = await _msSqlConnection.QueryAsync<string>(
                cancellationToken,
                GetSql,
                new {Key = key})
                .ConfigureAwait(false);

            return values.SingleOrDefault();
        }

        public async Task SetAsync(string key, string value, CancellationToken cancellationToken)
        {
            ValidateKey(key);
            ValidateValue(value);

            var affectedRows = await _msSqlConnection.ExecuteAsync(
                cancellationToken,
                SetSql,
                new {Key = key, Value = value})
                .ConfigureAwait(false);

            if (affectedRows != 1)
            {
                throw new Exception($"Updating key '{key}' didn't update any rows");
            }
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken)
        {
            ValidateKey(key);

            return _msSqlConnection.ExecuteAsync(
                cancellationToken,
                DeleteSql,
                new {Key = key});
        }

        private static void ValidateKey(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (key.Length > 128) throw new ArgumentOutOfRangeException(nameof(key), $"Key '{key}' is too long, it can only be 128");
        }

        private static void ValidateValue(string value)
        {
            if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value));
        }
    }
}