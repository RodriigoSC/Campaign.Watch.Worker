using CampaignWatchWorker.Application.Resolver;
using CampaignWatchWorker.Application.Services.Interfaces;
using CampaignWatchWorker.Data.Factories;
using CampaignWatchWorker.Data.Factories.Common;
using CampaignWatchWorker.Data.Resolver;
using CampaignWatchWorker.Data.Services;
using CampaignWatchWorker.Domain.Models.Configuration;
using CampaignWatchWorker.Domain.Models.Interfaces;
using CampaignWatchWorker.Domain.Models.Interfaces.Services;
using CampaignWatchWorker.Domain.Models.Interfaces.Services.Email;
using CampaignWatchWorker.Domain.Models.Interfaces.Services.Read.Campaign;
using CampaignWatchWorker.Domain.Models.Interfaces.Services.Scheduler;
using CampaignWatchWorker.Domain.Models.Interfaces.Services.Webhook;
using CampaignWatchWorker.Infra.Campaign.Factories;
using CampaignWatchWorker.Infra.Campaign.Resolver;
using CampaignWatchWorker.Infra.Campaign.Services;
using CampaignWatchWorker.Infra.MultiTenant;
using CampaignWatchWorker.Infra.Services.Email;
using CampaignWatchWorker.Infra.Services.Scheduler;
using CampaignWatchWorker.Infra.Services.Webhook;
using DTM_Logging.Ioc;
using DTM_Vault.Data;
using DTM_Vault.Data.Factory;
using DTM_Vault.Data.KeyValue;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace CampaignWatchWorker.Infra.Ioc
{
    public class Bootstrap
    {
        public static async Task StartIoC(IServiceCollection services, IConfiguration configuration, string applicationName)
        {
            Console.WriteLine("[Bootstrap] 1. Iniciando DI...");

            var environment = ValidateIfNull(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "ASPNETCORE_ENVIRONMENT");
            var conn_string_vault = ValidateIfNull(Environment.GetEnvironmentVariable("CONN_STRING_VAULT"), "CONN_STRING_VAULT");
            var user_vault = ValidateIfNull(Environment.GetEnvironmentVariable("USER_VAULT"), "USER_VAULT");
            var pass_vault = ValidateIfNull(Environment.GetEnvironmentVariable("PASS_VAULT"), "PASS_VAULT");
            var pathLog = ValidateIfNull(configuration["PathLog"], "pathLog");

            Console.WriteLine($"[Bootstrap] 2. Conectando ao Vault ({conn_string_vault})...");
            var vaultFactory = VaultFactory.CreateInstance(conn_string_vault, user_vault, pass_vault);
            var vaultService = new VaultService(vaultFactory);

            services.AddSingleton<IVaultFactory>(vaultFactory);
            services.AddTransient<IVaultService, VaultService>();

            var vaultKeyPath = $"monitoring/{environment}/data/keys";

            Console.WriteLine("[Bootstrap] 3. Buscando configurações no Vault...");
            var rabbitHost = await vaultService.GetKeyAsync(vaultKeyPath, "RabbitMQ.host");
            var rabbitUser = await vaultService.GetKeyAsync(vaultKeyPath, "RabbitMQ.user");
            var rabbitPass = await vaultService.GetKeyAsync(vaultKeyPath, "RabbitMQ.pass");
            var rabbitVHost = await vaultService.GetKeyAsync(vaultKeyPath, "RabbitMQ.virtualhost");
            var queueName = await vaultService.GetKeyAsync(vaultKeyPath, "RabbitMQ.QueueName");

            var schedulerApiUrl = await vaultService.GetKeyAsync(vaultKeyPath, "SchedulerApiUrl");

            var smtpHost = await vaultService.GetKeyAsync(vaultKeyPath, "SMTP.Host");
            var smtpPort = await vaultService.GetKeyAsync(vaultKeyPath, "SMTP.Port");
            var smtpUser = await vaultService.GetKeyAsync(vaultKeyPath, "SMTP.User");
            var smtpPass = await vaultService.GetKeyAsync(vaultKeyPath, "SMTP.Pass");
            var smtpFrom = await vaultService.GetKeyAsync(vaultKeyPath, "SMTP.FromAddress");

            services.AddSingleton<IConnection>(sp =>
            {
                var factory = new ConnectionFactory
                {
                    HostName = rabbitHost,
                    UserName = rabbitUser,
                    Password = rabbitPass,
                    VirtualHost = rabbitVHost,
                    DispatchConsumersAsync = true
                };

                return factory.CreateConnection();
            });

            services.AddSingleton(new WorkerSettings
            {
                QueueName = queueName,
                PrefetchCount = 5
            });

            services.AddScoped<ITenantContext, TenantContext>();

            services.AddSingleton<IMongoDbFactory>(sp =>
            {
                var vs = sp.GetRequiredService<IVaultService>();
                return new MongoDbFactory(vs, environment);
            });

            services.AddSingleton<IPersistenceMongoFactory>(sp =>
            {
                var mongoFactory = sp.GetRequiredService<IMongoDbFactory>();
                var vs = sp.GetRequiredService<IVaultService>();
                var dbName = vs.GetKeyAsync(vaultKeyPath, "MongoDB.Persistence.database").GetAwaiter().GetResult();
                return new PersistenceMongoFactory(mongoFactory, dbName);
            });           

            services.AddHttpClient<ISchedulerApiService, SchedulerApiService>(client =>
            {
                client.BaseAddress = new Uri(schedulerApiUrl);
            });

            var smtpSettings = new SmtpSettings
            {
                Host = smtpHost,
                Port = int.TryParse(smtpPort, out int port) ? port : 25,
                UserName = smtpUser,
                Password = smtpPass,
                FromAddress = smtpFrom,
                FromName = "Campaign Watch"
            };

            services.AddSingleton(smtpSettings);
            services.AddTransient<IEmailDispatcherService, EmailDispatcherService>();
            services.AddTransient<IWebhookDispatcherService, WebhookDispatcherService>();

            services.AddScoped<ICampaignMongoFactory, CampaignMongoFactory>();
            services.AddSingleton<IClientConfigService, ClientConfigService>();
            services.AddScoped<IChannelReadModelService, ChannelReadModelService>();
            services.AddRepositoryData();
            services.AddCampaignRepository();
            services.AddApplication();
            services.AddFileLogger(pathLog, applicationName, environment);

            Console.WriteLine("[Bootstrap] 4. Configuração concluída.");
        }

        private static string ValidateIfNull(string? value, string? name)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentNullException(nameof(value), $"{name} não pode ser nulo");
            return value;
        }
    }
}