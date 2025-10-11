using CampaignWatchWorker.Domain.Models.Interfaces.Services.Read.Effwhatsapp;
using CampaignWatchWorker.Infra.Effwhatsapp.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CampaignWatchWorker.Infra.Effwhatsapp.Resolver
{
    public static class ResolverIoC
    {
        public static IServiceCollection AddEffwhatsappRepository(this IServiceCollection services)
        {
            services.AddTransient<IEffwhatsappReadModelService, EffwhatsappReadModelService>();

            return services;
        }
    }
}
