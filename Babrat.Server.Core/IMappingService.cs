namespace Babrat.Server.Core;

public interface IMappingService
{
    TDestination Map<TDestination>(object source);
}