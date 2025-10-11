using CampaignWatchWorker.Application.Processor;
using CampaignWatchWorker.Application.QueueEventHandler;
using Microsoft.Extensions.DependencyInjection;

namespace CampaignWatchWorker.Application.Resolver
{
    public static class ResolverIoC
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddTransient<IProcessorApplication, ProcessorApplication>();
            services.AddTransient<IQueueEventHandlerApplication, QueueEventHandlerApplication>();

            return services;
        }
    }
}
