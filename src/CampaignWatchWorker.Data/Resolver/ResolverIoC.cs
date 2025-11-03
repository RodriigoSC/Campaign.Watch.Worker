using CampaignWatchWorker.Data.Repositories;
using CampaignWatchWorker.Domain.Models.Interfaces.Repositories;
using CampaignWatchWorker.Domain.Models.Interfaces.Repositories.Alerts;
using Microsoft.Extensions.DependencyInjection;

namespace CampaignWatchWorker.Data.Resolver
{
    public static class ResolverIoC
    {
        public static IServiceCollection AddRepositoryData(this IServiceCollection services)
        {
            services.AddTransient<ICampaignModelRepository, CampaignModelRepository>();
            services.AddTransient<IExecutionModelRepository, ExecutionModelRepository>();

            services.AddTransient<IAlertConfigurationRepository, AlertConfigurationRepository>();
            services.AddTransient<IAlertHistoryRepository, AlertHistoryRepository>();


            return services;
        }
    }
}
