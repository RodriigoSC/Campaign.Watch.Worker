using CampaignWatchWorker.Application.Mappers;
using CampaignWatchWorker.Application.Processor;
using CampaignWatchWorker.Application.QueueEventHandler;
using CampaignWatchWorker.Application.Validators;
using Microsoft.Extensions.DependencyInjection;

namespace CampaignWatchWorker.Application.Resolver
{
    public static class ResolverIoC
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // Processamento
            services.AddTransient<IProcessorApplication, ProcessorApplication>();
            services.AddTransient<IQueueEventHandlerApplication, QueueEventHandlerApplication>();

            // Mapeamento
            services.AddTransient<ICampaignMapper, CampaignMapper>();

            // Análise de saúde
            services.AddTransient<ICampaignHealthAnalyzer, CampaignHealthAnalyzer>();

            // Validadores de Steps
            services.AddTransient<IStepValidator, FilterStepValidator>();
            services.AddTransient<IStepValidator, ChannelStepValidator>();
            services.AddTransient<IStepValidator, EndStepValidator>();

            services.AddTransient<IStepValidator, WaitStepValidator>();

            return services;
        }
    }
}
