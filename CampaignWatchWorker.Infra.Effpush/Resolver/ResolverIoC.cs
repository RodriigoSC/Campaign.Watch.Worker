using CampaignWatchWorker.Domain.Models.Interfaces.Services.Read.Effpush;
using CampaignWatchWorker.Infra.Effpush.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CampaignWatchWorker.Infra.Effpush.Resolver
{
    public static class ResolverIoC
    {
        public static IServiceCollection AddEffpushRepository(this IServiceCollection services)
        {
            services.AddTransient<IEffpushReadModelService, EffpushReadModelService>();

            return services;
        }
    }
}
