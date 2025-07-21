using Babrat.Domain.Models;

namespace Babrat.Server.Core;

public interface IQueryProcessor
{
    Task<HttpResponseMessage> ProcessAsync(SqlQueryResult model, CancellationToken ct);
}
