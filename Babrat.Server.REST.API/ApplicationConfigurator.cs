using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Babrat.Server.Core;
using Babrat.Server.Gateway.REST.API.Settings;
using Babrat.Server.Grpc.Models;
using Babrat.Server.REST.API.Controller;

using Babrat.Server.REST.API.Settings;
using FileNet.Api.Authentication;
using Grpc.Core;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Babrat.Server.REST.API;

public class ApplicationConfigurator : IApplicationConfigurator, IStartup
{
    #region RGU.WebProgramming.Server.Core.IApplicationConfigurator implementation
    
    /// <inheritdoc cref="IApplicationConfigurator.Configure" /> 
    public void Configure(
        IApplicationBuilder applicationBuilder)
    {
        
        applicationBuilder.UseRouting();
        
        applicationBuilder.UseAuthentication();
        applicationBuilder.UseAuthorization();
        
        applicationBuilder.UseEndpoints(endpointRouteBuilder =>
        {
            endpointRouteBuilder.MapControllers();
        });
        
    }
    
    #endregion
    
    #region RGU.WebProgramming.Server.Core.IStartup implementation
    
    /// <inheritdoc cref="Core.IStartup.ConfigureServices" />
    public void ConfigureServices(
        HostBuilderContext ctx,
        IServiceCollection services)
    {
        services
            .Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 524288000;
            })
            .AddControllers()
            .AddApplicationPart(typeof(FileDbController).Assembly)
            .AddNewtonsoftJson();
        
        services.AddMapster();
        
        var externalApiSettings = new ExternalApiSettings();
        
        ctx.Configuration.Bind(nameof(ExternalApiSettings), externalApiSettings);

        
        services.AddGrpcClient<CreateTableService.CreateTableServiceClient>(o =>
            {
                o.Address = new Uri($"{(!string.IsNullOrEmpty(externalApiSettings.CertPath) ? "https" : "http")}://" +
                                        $"{externalApiSettings.BaseAddress}:" +
                                        $"{externalApiSettings.BasePort}");
            })
            .ConfigureChannel(o =>
            {
                o.Credentials = ChannelCredentials.Insecure;
            });
        
        services.AddGrpcClient<InsertService.InsertServiceClient>(o =>
            {
                o.Address = new Uri($"{(!string.IsNullOrEmpty(externalApiSettings.CertPath) ? "https" : "http")}://" +
                                    $"{externalApiSettings.BaseAddress}:" +
                                    $"{externalApiSettings.BasePort}");
            })
            .ConfigureChannel(o =>
            {
                o.Credentials = ChannelCredentials.Insecure;
            });
        
        
        services.Configure<AuthApiSettings>(ctx.Configuration.GetSection(nameof(AuthApiSettings)));

        services.AddAuthorization();
        
        
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            var authApiSettings = services.BuildServiceProvider()
                .GetRequiredService<IOptions<AuthApiSettings>>().Value;
            
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true ,
                ValidateAudience = true ,
                ValidateLifetime = true ,
                ValidateIssuerSigningKey = true ,
                ValidIssuer = authApiSettings.Issuer,
                ValidAudience = authApiSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authApiSettings.Secret ))
            };
            
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception is SecurityTokenExpiredException)
                    {
                        context.Response.Headers.Append("Token-Expired", "true");
                    }
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
            
                    var result = JsonSerializer.Serialize(
                        new { error = "Invalid token resource" });
                    return context.Response.WriteAsync(result);
                }
            };
            
        });

        
        services.AddStackExchangeRedisCache(options =>
        {
            var redisSettings = ctx.Configuration.GetSection(nameof(RedisSettings)).Get<RedisSettings>();
            options.Configuration = $"{redisSettings.BaseAddress}:{redisSettings.BasePort}";
        });

    }
    
    #endregion
    
    
}

// services.AddHttpClient("ExternalApi")
//     .ConfigurePrimaryHttpMessageHandler(() =>
//     {
//         var handler = new HttpClientHandler();
//         
//         if (!string.IsNullOrEmpty(externalApiSettings.CertPath))
//         {
//             if (!File.Exists(externalApiSettings.CertPath))
//             {
//                 throw new FileNotFoundException("Certificate file not found.",
//                     externalApiSettings.CertPath);
//             }
//             
//             var certificate = new X509Certificate2(
//                 externalApiSettings.CertPath,
//                 externalApiSettings.CertPassword);
//
//             handler.ClientCertificates.Add(certificate);
//             
//             //for ignore error
//             // handler.ServerCertificateCustomValidationCallback = 
//             //     HttpClientHandler.DangerousAcceptAnyServerCertificateVal
//         }
//
//         return handler;
//     }) 
//     .ConfigureHttpClient(client =>
//     {
//        
//         client.BaseAddress = new Uri($"{(!string.IsNullOrEmpty(externalApiSettings.CertPath) ? "https" : "http")}://" +
//                                      $"{externalApiSettings.BaseAddress}:" +
//                                      $"{externalApiSettings.BasePort}");
//
//         client.DefaultRequestHeaders.Accept.Add(
//             new MediaTypeWithQualityHeaderValue("application/json"));
//
//         client.Timeout = TimeSpan.FromSeconds(externalApiSettings.TimeoutSeconds);
//     });