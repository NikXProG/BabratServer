using Microsoft.AspNetCore.Hosting;

namespace Babrat.Server.Core;

public interface IWebHostConfigurator
{
   
   void Configure(IWebHostBuilder webHostBuilder);
   
}