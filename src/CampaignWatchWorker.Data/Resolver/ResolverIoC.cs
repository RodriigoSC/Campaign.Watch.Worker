using CampaignWatchWorker.Data.Repositories;
using CampaignWatchWorker.Domain.Models.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace CampaignWatchWorker.Data.Resolver
{
    public static class ResolverIoC
    {
        public static IServiceCollection AddRepositoryData(this IServiceCollection services)
        {
            services.AddTransient<ICampaignModelRepository, CampaignModelRepository>();
            services.AddTransient<IExecutionModelRepository, ExecutionModelRepository>();


            return services;
        }
    }
}
