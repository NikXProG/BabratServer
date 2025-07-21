using System.Net;
using Babrat.Domain.Models;
using Babrat.Server.Core;

namespace Babrat.Server.REST.API;

public class QueryProcessor : IQueryProcessor
{
    private readonly IEnumerable<IQueryHandler> _handlers;

    public QueryProcessor(IEnumerable<IQueryHandler> handlers)
    {
        _handlers = handlers;
    }

    public async Task<HttpResponseMessage> ProcessAsync(SqlQueryResult model, CancellationToken ct)
    {
        var handler = _handlers.FirstOrDefault(h => h.CanHandle(model));
        if (handler == null)
        {
            return new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent($"Unsupported SQL query type: {model.GetType().Name}")
            };
        }

        return await handler.HandleAsync(model, ct);
    }
}