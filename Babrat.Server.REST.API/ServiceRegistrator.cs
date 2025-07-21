using System.Reflection;
using Babrat.Domain.Models;
using Babrat.Server.Core;
using Babrat.Server.Gateway.REST.API;
using Babrat.Server.REST.API.Controller;
using Babrat.Server.REST.API.Handler;
using Babrat.Server.REST.API.Mapping;
using DryIoc;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Configuration;

namespace Babrat.Server.REST.API;

public sealed class ServiceRegistrator:
    IServiceRegistrator
{
    
    #region RGU.WebProgramming.Server.Core.IServiceRegistrator implementation
    
    /// <inheritdoc cref="IServiceRegistrator.Register" />
    public void Register(
        IRegistrator registrator,
        IConfiguration configuration)
    {
        
        registrator.RegisterMany<ApplicationConfigurator>(Reuse.Singleton);
        registrator.Register<IWebHostConfigurator, WebHostConfigurator>(Reuse.Singleton);
        
        registrator.Register<FileDbController>(Reuse.Singleton);
        registrator.Register<IQueryProcessor, QueryProcessor>(Reuse.Singleton);
        
        registrator.Register<IQueryHandler, TableQueryHandler>(Reuse.Transient);
        registrator.Register<IQueryHandler, InsertQueryHandler>(Reuse.Transient);
        
        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(Assembly.GetExecutingAssembly());
        registrator.RegisterInstance(config);
        
    }
    
    #endregion
    
}