using CampaignWatchWorker.Data.Repositories;
using CampaignWatchWorker.Data.Services;
using CampaignWatchWorker.Domain.Models.Interfaces.Repositories;
using CampaignWatchWorker.Domain.Models.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CampaignWatchWorker.Data.Resolver
{
    public static class ResolverIoC
    {
        public static IServiceCollection AddRepositoryData(this IServiceCollection services)
        {
            services.AddTransient<ICampaignModelRepository, CampaignModelRepository>();
            services.AddTransient<ICampaignModelService, CampaignModelService>();

            return services;
        }
    }
}
