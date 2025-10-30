using CampaignWatchWorker.Application.Resolver;
using CampaignWatchWorker.Data.Factories;
using CampaignWatchWorker.Data.Factories.Common;
using CampaignWatchWorker.Data.Resolver;
using CampaignWatchWorker.Data.Services;
using CampaignWatchWorker.Domain.Models.Interfaces; 
using CampaignWatchWorker.Domain.Models.Interfaces.Services;
using CampaignWatchWorker.Domain.Models.Interfaces.Services.Read.Campaign;
using CampaignWatchWorker.Infra.Campaign.Factories;
using CampaignWatchWorker.Infra.Campaign.Resolver;
using CampaignWatchWorker.Infra.Campaign.Services;
using CampaignWatchWorker.Infra.MultiTenant;
using DTM_Logging.Ioc;
using DTM_Vault.Data;
using DTM_Vault.Data.Factory;
using DTM_Vault.Data.KeyValue;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CampaignWatchWorker.Infra.Ioc
{
    public class Bootstrap
    {
        public static async Task StartIoC(IServiceCollection services, IConfiguration configuration, string applicationName)
        {
            var environment = ValidateIfNull(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "ASPNETCORE_ENVIRONMENT");
            var conn_string_vault = ValidateIfNull(Environment.GetEnvironmentVariable("CONN_STRING_VAULT"), "CONN_STRING_VAULT");
            var user_vault = ValidateIfNull(Environment.GetEnvironmentVariable("USER_VAULT"), "USER_VAULT");
            var pass_vault = ValidateIfNull(Environment.GetEnvironmentVariable("PASS_VAULT"), "PASS_VAULT");
            var pathLog = ValidateIfNull(configuration["PathLog"], "pathLog");

            services.AddSingleton<IVaultFactory>(_ => VaultFactory.CreateInstance(conn_string_vault, user_vault, pass_vault));
            services.AddTransient<IVaultService, VaultService>();

            services.AddScoped<ITenantContext, TenantContext>();

            services.AddSingleton<IMongoDbFactory>(sp =>
            {
                var vaultService = sp.GetRequiredService<IVaultService>();
                return new MongoDbFactory(vaultService, environment);
            });

            services.AddSingleton<IPersistenceMongoFactory>(sp =>
            {
                var mongoFactory = sp.GetRequiredService<IMongoDbFactory>();
                var vaultService = sp.GetRequiredService<IVaultService>();
                var dbName = vaultService.GetKeyAsync($"monitoring/{environment}/data/keys", "MongoDB.Persistence.database").GetAwaiter().GetResult();
                return new PersistenceMongoFactory(mongoFactory, dbName);
            });
            
            services.AddScoped<ICampaignMongoFactory, CampaignMongoFactory>();
            
            services.AddSingleton<IClientConfigService, ClientConfigService>();

            services.AddScoped<IChannelReadModelService, ChannelReadModelService>();

            services.AddRepositoryData(); 

            services.AddCampaignRepository();

            services.AddApplication();

            services.AddFileLogger(pathLog, applicationName, environment);
        }

        private static string ValidateIfNull(string? value, string? name)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentNullException(nameof(value), $"{name} não pode ser nulo");
            return value;
        }
    }
}