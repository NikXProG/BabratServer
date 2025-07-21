using System.Net;
using System.Text.Json;
using Babrat.Domain.Models;
using Babrat.Server.Core;
using Babrat.Server.Grpc.Models;
using Grpc.Core;
using MapsterMapper;

namespace Babrat.Server.REST.API.Handler;


public class InsertQueryHandler : IQueryHandler
{
    private readonly InsertService.InsertServiceClient _clientApiFactory;
    private readonly IMapper _mapper;
    
    public InsertQueryHandler(
        InsertService.InsertServiceClient clientApiFactory, 
        IMapper mapper)
    {
        _clientApiFactory = clientApiFactory;
        _mapper = mapper;
    }
    
    public bool CanHandle(SqlQueryResult model) => model is Domain.Models.InsertModel;
    
    public async Task<HttpResponseMessage> HandleAsync(SqlQueryResult model, CancellationToken ct)
    {
        var insertModel = (Domain.Models.InsertModel)model;
        
        foreach(var i in insertModel.Rows[0].Values)
        {
            Console.WriteLine(i);
        }

        var imodel = _mapper.Map<Grpc.Models.InsertModel>(insertModel);


        foreach (var insert in imodel.Rows)
        {
            foreach (var insertValue in insert.Values)
            {
                Console.WriteLine(insertValue);
            }
        }
        var response = await _clientApiFactory.InsertIntoAsync(
            _mapper.Map<Grpc.Models.InsertModel>(insertModel), 
            new CallOptions(cancellationToken: ct));
        
        Console.WriteLine(JsonSerializer.Serialize(response));
        
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(response))
        };
    
    } 
            
}