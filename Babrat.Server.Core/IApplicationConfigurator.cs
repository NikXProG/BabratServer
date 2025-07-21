using Microsoft.AspNetCore.Builder;

namespace Babrat.Server.Core;

/// <summary>
/// 
/// </summary>
public interface IApplicationConfigurator
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="applicationBuilder"></param>
    void Configure(
        IApplicationBuilder applicationBuilder);
    
}