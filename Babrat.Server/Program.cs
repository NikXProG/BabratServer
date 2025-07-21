using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Reflection;
using Babrat.Server.Deployment;

namespace Babrat.Server
{

    internal class Program
    {

        public static int Main(string[] args)
        {

            var configuration = BuildConfiguration();

            Log.Logger = BuildLogger(configuration);

            try
            {

                var host = Startup.CreateHostBuilder().Build();

                Log.Logger?.Information("Application is starting up...");

                var container = host.Services.GetService<IContainer>()
                    .WithCompositionRoot<ServicesRegistration>();
    
                Log.Logger?.Information(
                    "Dependency container has been initialized and composition root has been set up.");

                var app = container.Resolve<App>();

                Log.Logger?.Information("Application resolved from the container. Starting application...");

                app.Start();

            }
            catch (Exception ex)
            {
                Log.Logger?.Fatal("Application start up failed");
                throw;
            }
            finally
            {
                Log.CloseAndFlush();
            }

            return 0;


        }

        #region Private Methods

        private static IConfiguration BuildConfiguration()
        {
            var rootPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("BASEDIR", rootPath);

            //obsolete code
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");


            return new ConfigurationBuilder()
                .SetBasePath(rootPath)
                .AddJsonFile($"appsettings.json", false, true)
                .AddJsonFile($"appsettings.{environment}.json", true)
                .AddJsonFile($"appsettings.{environment}.User.json", true)
                .Build();

        }

        private static ILogger BuildLogger(IConfiguration configuration)
        {
            return new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();
        }

        #endregion



    }
}
