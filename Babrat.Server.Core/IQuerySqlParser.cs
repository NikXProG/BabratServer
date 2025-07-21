using Babrat.Domain.Models;

namespace Babrat.Server.Core;

public interface IQuerySqlParser
{
    Task<SqlQueryResult> ParseAsync(string query, CancellationToken ct = default);
}