using System.Reflection;
using Babrat.Server.Core;
using Babrat.Server.Gateway.Settings;
using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Babrat.Server.Deployment;

public class Startup
{
    #region Fields
    
    /// <summary>
    /// Stores the root directory of the application.
    /// </summary>
    private static string _rootPath;

    /// <summary>
    /// Stores module information (REST, gRPC).
    /// Keys are module names, values contain details like whether they are enabled and their assembly file.
    /// </summary>
    private static Dictionary<string, ModuleInfo>? _modules;

    #endregion
    
    #region Methods
    
    /// <summary>
    /// Creates and configures a new host builder for the application.
    /// The method configures services, logging, modules, and environment settings.
    /// </summary>
    /// <param name="rootPath">
    /// The root directory path for the application. If null, the default directory will be used. 
    /// This path is used to set the base path for configuration files and other resources.
    /// </param>
    /// <param name="additionalConfigureServices">
    /// An optional delegate that allows additional configuration of services. 
    /// It takes the <see cref="HostBuilderContext"/> and <see cref="IServiceCollection"/> as parameters.
    /// </param>
    /// <returns>
    /// Returns an <see cref="IHostBuilder"/> configured with services, logging, and environment-specific settings.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when there is an attempt to register multiple <see cref="IApplicationConfigurator"/> instances, 
    /// which is not supported in this application. Only one instance of <see cref="IApplicationConfigurator"/> is allowed.
    /// </exception>
    public static IHostBuilder CreateHostBuilder(
        string rootPath = null,
        Action<HostBuilderContext, IServiceCollection> additionalConfigureServices = null)
    {
        Directory.SetCurrentDirectory(_rootPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

        return new HostBuilder()
            .UseSerilog()
            .ConfigureWebHostDefaults(webHostBuilder =>
        {
            webHostBuilder
                .ConfigureAppConfiguration((context, configurationBuilder) =>
                {
                    configurationBuilder
                        .SetBasePath(_rootPath)
                        .AddJsonFile("appsettings.json", false, true)
                        .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", true, true);
                    
                    if (context.HostingEnvironment.IsDevelopment())
                    {
                        configurationBuilder.AddJsonFile(
                            $"appsettings.{context.HostingEnvironment.EnvironmentName}.User.json", true);
                    }

                    configurationBuilder
                        .AddEnvironmentVariables()
                        .Build()
                        .Bind("Modules", _modules = new Dictionary<string, ModuleInfo>());

                    foreach (var enabledModuleInfo in _modules.Where(module => module.Value.Enabled))
                    {
                        Log.Logger?.Information("Adding module \"{moduleKey}\" from assembly \"{moduleAssembly}\"",
                            enabledModuleInfo.Key, enabledModuleInfo.Value.AssemblyName);
                        Assembly.LoadFrom(enabledModuleInfo.Value.AssemblyName);
                    }
                    
                    var webHostConfigurators = AppDomain.CurrentDomain.GetAssemblies()
                        .Distinct()
                        .SelectMany(assembly => assembly.DefinedTypes)
                        .Where(assemblyDefinedType => assemblyDefinedType.ImplementsServiceType<IWebHostConfigurator>())
                        .Select(webHostConfiguratorType => (IWebHostConfigurator)Activator.CreateInstance(webHostConfiguratorType)!)
                        .ToList();
                    
                    foreach (var webHostConfigurator in webHostConfigurators)
                    {
                        webHostConfigurator.Configure(webHostBuilder);
                    }
                    
                })
                .Configure((_, applicationBuilder) =>
                {
                    var applicationConfigurators = AppDomain.CurrentDomain.GetAssemblies()
                        .Distinct()
                        .SelectMany(assembly => assembly.DefinedTypes)
                        .Where(assemblyDefinedType => assemblyDefinedType.ImplementsServiceType<IApplicationConfigurator>())
                        .Select(applicationConfiguratorType => (IApplicationConfigurator)Activator.CreateInstance(applicationConfiguratorType)!)
                        .ToList();
                        
                    // TODO: deprecated/obsolete code
                    /*if (applicationConfigurators.Count > 1)
                    {
                        throw new InvalidOperationException("Only one IApplicationConfigurator instance is supported");
                    }*/
                        
                    foreach (var applicationConfigurator in applicationConfigurators)
                    {
                        applicationConfigurator.Configure(applicationBuilder);
                    }
                });
            })
            .ConfigureHostConfiguration(hostBuilder =>
            {
                hostBuilder.AddEnvironmentVariables();
            })
            .UseServiceProviderFactory(hostBuilderContext =>
                new DryIocServiceProviderFactory(hostBuilderContext.Properties["DryIocContainer"] as IContainer))
            .ConfigureServices((hostBuilderContext, serviceCollection) =>
            {
                ConfigureServices(hostBuilderContext, serviceCollection);
                additionalConfigureServices?.Invoke(hostBuilderContext, serviceCollection);
                IContainer container = new Container();
                container.RegisterInstance(_modules);
                container.RegisterInstance(hostBuilderContext.Configuration);
                container = container
                    .WithCompositionRoot<ServicesRegistration>()
                    .WithCompositionRoot<ServiceRegistratorsCompositionRoot>();
                
                var startups = container.ResolveMany<Core.IStartup>();
                foreach (var startup in startups ?? [])
                {
                    startup.ConfigureServices(hostBuilderContext, serviceCollection);
                }

                hostBuilderContext.Properties["DryIocContainer"] = container;
            });
    }
    
    #endregion
    
    private static void ConfigureServices(
        HostBuilderContext ctx,
        IServiceCollection services)
    {
        CatchUnhandledExceptions();
        
        services
            .AddOptions()
            .Configure<ModuleInfo[]>(ctx.Configuration.GetSection("Modules"));
    }

    private static void CatchUnhandledExceptions()
    {
        // log all unhandled exceptions messages into console
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            Log.Logger?.Fatal($"Unhandled exception occured: {(e.ExceptionObject as Exception)?.Message}");
        };
    }
    
    
    
}