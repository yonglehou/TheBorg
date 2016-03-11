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
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Serilog;

namespace TheBorg.Collective.MsSql
{
    public abstract class MsSqlConnection : IMsSqlConnection
    {
        public virtual Task<int> ExecuteAsync(
            CancellationToken cancellationToken,
            string sql,
            object param = null)
        {
            return WithConnectionAsync(
                (c, ct) =>
                {
                    var commandDefinition = new CommandDefinition(sql, param, cancellationToken: ct);
                    return c.ExecuteAsync(commandDefinition);
                },
                cancellationToken);
        }

        public virtual async Task<IReadOnlyCollection<TResult>> QueryAsync<TResult>(
            CancellationToken cancellationToken,
            string sql,
            object param = null)
        {
            return (
                await WithConnectionAsync(
                    (c, ct) =>
                    {
                        var commandDefinition = new CommandDefinition(sql, param, cancellationToken: ct);
                        return c.QueryAsync<TResult>(commandDefinition);
                    },
                    cancellationToken)
                    .ConfigureAwait(false))
                .ToList();
        }

        public virtual Task<IReadOnlyCollection<TResult>> InsertMultipleAsync<TResult, TRow>(
            CancellationToken cancellationToken,
            string sql,
            IEnumerable<TRow> rows)
            where TRow : class, new()
        {
            Log.Debug(
                "Insert multiple not optimised, inserting one row at a time using SQL '{0}'",
                sql);

            return WithConnectionAsync<IReadOnlyCollection<TResult>>(
                async (c, ct) =>
                {
                    using (var transaction = c.BeginTransaction())
                    {
                        try
                        {
                            var results = new List<TResult>();
                            foreach (var row in rows)
                            {
                                var commandDefinition = new CommandDefinition(sql, row, cancellationToken: ct);
                                var result = await c.QueryAsync<TResult>(commandDefinition).ConfigureAwait(false);
                                results.Add(result.First());
                            }
                            transaction.Commit();
                            return results;
                        }
                        catch (Exception)
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                },
                cancellationToken);
        }

        public virtual async Task<TResult> WithConnectionAsync<TResult>(
            Func<IDbConnection, CancellationToken, Task<TResult>> withConnection,
            CancellationToken cancellationToken)
        {
            using (var sqlConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["sql.connectionstring"].ConnectionString))
            {
                return await withConnection(sqlConnection, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}