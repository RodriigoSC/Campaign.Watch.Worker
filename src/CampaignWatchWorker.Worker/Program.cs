using System.Reflection;
using CampaignWatchWorker.Infra.Ioc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CampaignWatchWorker.Worker
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var environment = ValidateIfNull(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "enviromentName");

            Console.WriteLine($"ASPNETCORE_ENVIRONMENT: {environment}");

            var services = new ServiceCollection();
            var configurationBuilder = new ConfigurationBuilder();

            var builder = configurationBuilder
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", true, true)
                .AddEnvironmentVariables();

            IConfigurationRoot configuration = builder.Build();

            services.AddSingleton<IConfiguration>(provider => configuration);
            services.AddScoped<Consumer>();

            await Bootstrap.StartIoC(services, configuration, Assembly.GetExecutingAssembly().GetName().Name);

            var servicesProvider = services.BuildServiceProvider();

            servicesProvider.GetService<Consumer>()?.Start();
            Console.ReadLine();
            Console.WriteLine("Parando excecução...");

            string ValidateIfNull(string? value, string? name)
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentNullException($"{name} cannot be null");

                return value;
            }
        }
    }
}