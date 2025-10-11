using CampaignWatchWorker.Application.Resolver;
using CampaignWatchWorker.Data.Factories;
using CampaignWatchWorker.Data.Factories.Common;
using CampaignWatchWorker.Data.Resolver;
using CampaignWatchWorker.Domain.Models.Interfaces;
using CampaignWatchWorker.Infra.Campaign.Factories;
using CampaignWatchWorker.Infra.Campaign.Resolver;
using CampaignWatchWorker.Infra.Effmail.Factories;
using CampaignWatchWorker.Infra.Effmail.Resolver;
using CampaignWatchWorker.Infra.Effpush.Factories;
using CampaignWatchWorker.Infra.Effpush.Resolver;
using CampaignWatchWorker.Infra.Effsms.Factories;
using CampaignWatchWorker.Infra.Effsms.Resolver;
using CampaignWatchWorker.Infra.Effwhatsapp.Factories;
using CampaignWatchWorker.Infra.Effwhatsapp.Resolver;
using CampaignWatchWorker.Infra.MultiTenant;
using DTM_Consul.Data.Factory;
using DTM_Consul.Data.KeyValue;
using DTM_MessageQueue.RabbitMQ.Factory;
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
            var tenantId = ValidateIfNull(Environment.GetEnvironmentVariable("TENANT"), "TENANT");

            services.AddSingleton<IVaultFactory>(_ => VaultFactory.CreateInstance(conn_string_vault, user_vault, pass_vault));
            services.AddTransient<IVaultService, VaultService>();

            services.AddSingleton<ISVFactory>(sp =>
            {
                var vaultService = sp.GetRequiredService<IVaultService>();
                var consulAddress = vaultService.GetKeyAsync($"monitoring/{environment}/data/keys", "Consul").GetAwaiter().GetResult();
                var consulToken = vaultService.GetKeyAsync($"monitoring/{environment}/data/keys", "Consul.token").GetAwaiter().GetResult();
                return SVFactory.CreateInstance(consulAddress);
            });

            services.AddTransient<IKVRepository, KVRepository>();

            services.AddSingleton<ITenant>(sp =>
            {
                var consulKvRepository = sp.GetRequiredService<IKVRepository>();
                var tenantResult = consulKvRepository.Get<Tenant>($"tenants/{tenantId}").GetAwaiter().GetResult();
                return TenantResolver.SetupTenant(tenantResult);
            });

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

            services.AddSingleton<ICampaignMongoFactory>(sp => new CampaignMongoFactory(sp.GetRequiredService<IMongoDbFactory>(), sp.GetRequiredService<ITenant>().DatabaseCampaign));
            services.AddSingleton<IEffmailMongoFactory>(sp => new EffmailMongoFactory(sp.GetRequiredService<IMongoDbFactory>(), sp.GetRequiredService<ITenant>().DatabaseEffmail));
            services.AddSingleton<IEffsmsMongoFactory>(sp => new EffsmsMongoFactory(sp.GetRequiredService<IMongoDbFactory>(), sp.GetRequiredService<ITenant>().DatabaseEffsms));
            services.AddSingleton<IEffpushMongoFactory>(sp => new EffpushMongoFactory(sp.GetRequiredService<IMongoDbFactory>(), sp.GetRequiredService<ITenant>().DatabaseEffpush));
            services.AddSingleton<IEffwhatsappMongoFactory>(sp => new EffwhatsappMongoFactory(sp.GetRequiredService<IMongoDbFactory>(), sp.GetRequiredService<ITenant>().DatabaseEffwhatsapp));


            services.AddRepositoryData();
            services.AddCampaignRepository();
            services.AddEffmailRepository();
            services.AddEffsmsRepository();
            services.AddEffpushRepository();
            services.AddEffwhatsappRepository();
            services.AddApplication();

            services.AddSingleton(x =>
            {
                var rabbitHost = x.GetService<IVaultService>()?.GetKeyAsync($"monitoring/{environment}/data/keys", "RabbitMQ.host");
                var rabbitUser = x.GetService<IVaultService>()?.GetKeyAsync($"monitoring/{environment}/data/keys", "RabbitMQ.user");
                var rabbitVirtualhost = x.GetService<IVaultService>()?.GetKeyAsync($"monitoring/{environment}/data/keys", "RabbitMQ.virtualhost");

                return DTM_RabbitMqFactory.CreateInstance($@"amqp://{rabbitUser?.Result}:{x.GetService<IVaultService>()?.GetKeyAsync($"monitoring/{environment}/data/keys", "RabbitMQ.pass")?.Result}@{rabbitHost?.Result}/{rabbitVirtualhost?.Result}".ToString());
            });
        }

        private static string ValidateIfNull(string? value, string? name)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentNullException(nameof(value), $"{name} não pode ser nulo");
            return value;
        }
    }
}