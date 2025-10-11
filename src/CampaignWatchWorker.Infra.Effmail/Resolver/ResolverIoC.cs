using CampaignWatchWorker.Domain.Models.Interfaces.Services.Read.Effmail;
using CampaignWatchWorker.Infra.Effmail.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CampaignWatchWorker.Infra.Effmail.Resolver
{
    public static class ResolverIoC
    {
        public static IServiceCollection AddEffmailRepository(this IServiceCollection services)
        {
            services.AddTransient<IEffmailReadModelService, EffmailReadModelService>();

            return services;
        }
    }
}
