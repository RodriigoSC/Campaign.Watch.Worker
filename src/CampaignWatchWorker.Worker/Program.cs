using CampaignWatchWorker.Infra.Ioc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace CampaignWatchWorker.Worker
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var environment = ValidateIfNull(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "enviromentName");
            Console.WriteLine($"ASPNETCORE_ENVIRONMENT: {environment}");

            var builder = new HostBuilder()
                .ConfigureAppConfiguration((hostContext, config) =>
                {
                    config.SetBasePath(AppContext.BaseDirectory)
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{environment}.json", true, true)
                        .AddEnvironmentVariables();
                })
                /*.ConfigureLogging((hostContext, logging) =>
                {
                    logging.AddConfiguration(hostContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                })*/
                .ConfigureServices((hostContext, services) =>
                {
                    var configuration = hostContext.Configuration;
                    services.AddSingleton<IConfiguration>(configuration);
                    Bootstrap.StartIoC(services, configuration, Assembly.GetExecutingAssembly().GetName().Name).GetAwaiter().GetResult();

                    services.AddHostedService<SingleQueueRabbitWorker>();
                });

            await builder.RunConsoleAsync();

            Console.WriteLine("Parando execução...");
        }

        private static string ValidateIfNull(string? value, string? name)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentNullException($"{name} cannot be null");
            return value;
        }
    }
}