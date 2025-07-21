using System.Net;
using System.Text.Json;
using Babrat.Domain.Models;
using Babrat.Server.Core;
using Babrat.Server.Grpc.Models;
using Grpc.Core;
using MapsterMapper;

namespace Babrat.Server.REST.API.Handler;

public class TableQueryHandler : IQueryHandler
{
    private readonly CreateTableService.CreateTableServiceClient _clientApiFactory;
    private readonly IMapper _mapper;

    public TableQueryHandler(
        CreateTableService.CreateTableServiceClient clientApiFactory, 
        IMapper mapper)
    {
        _clientApiFactory = clientApiFactory;
        _mapper = mapper;
    }

    public bool CanHandle(SqlQueryResult model) => model is Domain.Models.CreateTableModel;

    public async Task<HttpResponseMessage> HandleAsync(SqlQueryResult model, CancellationToken ct)
    {
        var tableModel = (Domain.Models.CreateTableModel)model;
        var response = await _clientApiFactory.CreateTableAsync(
            _mapper.Map<Grpc.Models.CreateTableModel>(tableModel), 
            new CallOptions(cancellationToken: ct));
        
        Console.WriteLine(JsonSerializer.Serialize(response));
        
        Console.WriteLine(model.QueryResultType);
        
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(response))
        };
        
    } 
            
}