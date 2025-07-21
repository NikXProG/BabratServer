using Babrat.Domain.Models;

namespace Babrat.Server.Core;

public interface IQueryHandler
{
    bool CanHandle(SqlQueryResult model);
    Task<HttpResponseMessage> HandleAsync(SqlQueryResult model, CancellationToken ct);
}