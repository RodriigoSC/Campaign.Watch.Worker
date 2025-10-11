using CampaignWatchWorker.Application.Resolver;
using CampaignWatchWorker.Application.Services.Interfaces;
using CampaignWatchWorker.Data.Factories;
using CampaignWatchWorker.Data.Factories.Common;
using CampaignWatchWorker.Data.Resolver;
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
            var tenantId = ValidateIfNull(Environment.GetEnvironmentVariable("TENANT"), "tenant");

            // Configura o serviço e a fábrica do Vault como Singleton.
            services.AddSingleton<IVaultFactory>(_ =>
                VaultFactory.CreateInstance(conn_string_vault, user_vault, pass_vault));
            services.AddTransient<IVaultService, VaultService>();

            services.AddSingleton(x =>
            {
                var rabbitHost = x.GetService<IVaultService>()?.GetKeyAsync($"monitoring/{environment}/data/keys", "RabbitMQ.host");
                var rabbitUser = x.GetService<IVaultService>()?.GetKeyAsync($"monitoring/{environment}/data/keys", "RabbitMQ.user");
                var rabbitVirtualhost = x.GetService<IVaultService>()?.GetKeyAsync($"monitoring/{environment}/data/keys", "RabbitMQ.virtualhost");

                return DTM_RabbitMqFactory.CreateInstance($@"amqp://{rabbitUser?.Result}:{x.GetService<IVaultService>()?.GetKeyAsync($"monitoring/{environment}/data/keys", "RabbitMQ.pass")?.Result}@{rabbitHost?.Result}/{rabbitVirtualhost?.Result}".ToString());
            });


            // Registra a fábrica genérica de MongoDB como Singleton.
            services.AddSingleton<IMongoDbFactory>(sp =>
            {
                var vaultService = sp.GetRequiredService<IVaultService>();
                return new MongoDbFactory(vaultService, environment);
            });

            // Registra a fábrica específica para o banco de dados de persistência como Singleton.
            services.AddSingleton<IPersistenceMongoFactory>(sp =>
            {
                var mongoFactory = sp.GetRequiredService<IMongoDbFactory>();
                var vaultService = sp.GetRequiredService<IVaultService>();
                return new PersistenceMongoFactory(mongoFactory, vaultService, environment);
            });

            // Registra a fábrica específica para os bancos de dados de campanha como Singleton.
            services.AddSingleton<ICampaignMongoFactory>(sp =>
            {
                var mongoFactory = sp.GetRequiredService<IMongoDbFactory>();
                return new CampaignMongoFactory(mongoFactory);
            });

            // Registra a fábrica específica para os bancos de dados de emails como Singleton.
            services.AddSingleton<IEffmailMongoFactory>(sp =>
            {
                var mongoFactory = sp.GetRequiredService<IMongoDbFactory>();
                return new EffmailMongoFactory(mongoFactory);
            });

            // Registra a fábrica específica para os bancos de dados de sms como Singleton.
            services.AddSingleton<IEffsmsMongoFactory>(sp =>
            {
                var mongoFactory = sp.GetRequiredService<IMongoDbFactory>();
                return new EffsmsMongoFactory(mongoFactory);
            });

            // Registra a fábrica específica para os bancos de dados de push como Singleton.
            services.AddSingleton<IEffpushMongoFactory>(sp =>
            {
                var mongoFactory = sp.GetRequiredService<IMongoDbFactory>();
                return new EffpushMongoFactory(mongoFactory);
            });

            // Registra a fábrica específica para os bancos de dados de whatsapp como Singleton.
            services.AddSingleton<IEffwhatsappMongoFactory>(sp =>
            {
                var mongoFactory = sp.GetRequiredService<IMongoDbFactory>();
                return new EffwhatsappMongoFactory(mongoFactory);
            });

            // Chama os métodos de extensão de outras camadas para registrar suas respectivas dependências.
            services.AddRepositoryData();

            services.AddCampaignRepository();

            services.AddEffmailRepository();
            services.AddEffsmsRepository();
            services.AddEffpushRepository();
            services.AddEffwhatsappRepository();

            services.AddApplication();

        }
        private static string ValidateIfNull(string? value, string? name)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentNullException(nameof(value), $"{name} cannot be null");

            return value;
        }
    }
}