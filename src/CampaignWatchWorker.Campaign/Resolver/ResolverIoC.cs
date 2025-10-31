using CampaignWatchWorker.Domain.Models.Interfaces.Services.Read.Campaign;
using CampaignWatchWorker.Infra.Campaign.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CampaignWatchWorker.Infra.Campaign.Resolver
{
    public static class ResolverIoC
    {
        public static IServiceCollection AddCampaignRepository(this IServiceCollection services)
        {
            services.AddTransient<ICampaignReadModelService, CampaignReadModelService>();

            return services;
        }
    }
}
