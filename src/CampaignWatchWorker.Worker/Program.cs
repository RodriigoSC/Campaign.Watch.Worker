using System.Reflection;
using CampaignWatchWorker.Infra.Ioc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
                .ConfigureServices(async (hostContext, services) =>
                {
                    var configuration = hostContext.Configuration;

                    services.AddSingleton<IConfiguration>(configuration);

                    await Bootstrap.StartIoC(services, configuration, Assembly.GetExecutingAssembly().GetName().Name);

                    services.AddHostedService<MultiTenantPollingWorker>();
                });

            await builder.RunConsoleAsync();

            Console.WriteLine("Parando excecução...");
        }

        private static string ValidateIfNull(string? value, string? name)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentNullException($"{name} cannot be null");

            return value;
        }
    }
}