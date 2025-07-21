using System.Net;
using Babrat.Server.Core;
using Babrat.Server.Gateway.REST.API.Settings;
using Babrat.Server.REST.API.Settings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;

namespace Babrat.Server.REST.API;

public class WebHostConfigurator : IWebHostConfigurator
{
    public void Configure(IWebHostBuilder webHostBuilder)
    {
        webHostBuilder
            .ConfigureKestrel((context, options) =>
            {
                var serverSettings = new ServerSettings();
                context.Configuration.Bind(nameof(ServerSettings), serverSettings);

                options.Limits.MaxRequestBodySize = 524288000;
             
                options.Listen(
                    IPAddress.Parse(serverSettings.ListenAddress),
                    serverSettings.ListenPort,
                    listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http1;
                    });
            });
    }
}