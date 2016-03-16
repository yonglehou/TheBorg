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
using TheBorg.Interface.ValueObjects;

namespace TheBorg.Collective.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly IMsSqlConnection _msSqlConnection;
        private const string SetSql = @"
            UPDATE [dbo].[Settings] SET [Value] = @Value WHERE [Key] = @Key AND GroupKey = @GroupKey;
            IF @@ROWCOUNT = 0
            BEGIN
                INSERT [dbo].[Settings] ([Key], [Value], [GroupKey]) VALUES (@Key, @Value, @GroupKey);
            END";
        private const string GetSql = "SELECT [Value] FROM [dbo].[Settings] WHERE [Key] = @Key AND GroupKey = @GroupKey";
        private const string DeleteSql = "DELETE FROM [dbo].[Settings] WHERE [Key] = @Key AND GroupKey = @GroupKey";

        public SettingsService(
            IMsSqlConnection msSqlConnection)
        {
            _msSqlConnection = msSqlConnection;
        }

        public async Task<string> GetAsync(SettingKey settingKey, SettingGroupKey settingGroupKey, CancellationToken cancellationToken)
        {
            var values = await _msSqlConnection.QueryAsync<string>(
                cancellationToken,
                GetSql,
                new {Key = settingKey.Value, GroupKey = settingGroupKey.Value })
                .ConfigureAwait(false);

            return values.SingleOrDefault();
        }

        public async Task SetAsync(SettingKey settingKey, SettingGroupKey settingGroupKey, string value, CancellationToken cancellationToken)
        {
            ValidateValue(value);

            var affectedRows = await _msSqlConnection.ExecuteAsync(
                cancellationToken,
                SetSql,
                new {Key = settingKey.Value, Value = value, GroupKey = settingGroupKey.Value})
                .ConfigureAwait(false);

            if (affectedRows != 1)
            {
                throw new Exception($"Updating key '{settingKey}' didn't update any rows");
            }
        }

        public Task RemoveAsync(SettingKey settingKey, SettingGroupKey settingGroupKey, CancellationToken cancellationToken)
        {
            return _msSqlConnection.ExecuteAsync(
                cancellationToken,
                DeleteSql,
                new {Key = settingKey.Value, GroupKey = settingGroupKey.Value });
        }

        private static void ValidateValue(string value)
        {
            if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value));
        }
    }
}