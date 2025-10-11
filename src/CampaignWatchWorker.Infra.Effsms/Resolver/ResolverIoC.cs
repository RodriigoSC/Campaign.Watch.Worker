using CampaignWatchWorker.Domain.Models.Interfaces.Services.Read.Effsms;
using CampaignWatchWorker.Infra.Effsms.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CampaignWatchWorker.Infra.Effsms.Resolver
{
    public static class ResolverIoC
    {
        public static IServiceCollection AddEffsmsRepository(this IServiceCollection services)
        {
            services.AddTransient<IEffsmsReadModelService, EffsmsReadModelService>();

            return services;
        }
    }
}
