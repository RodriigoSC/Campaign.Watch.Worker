using CampaignWatchWorker.Application.Analyzer;
using CampaignWatchWorker.Application.Mappers;
using CampaignWatchWorker.Application.Processor;
using CampaignWatchWorker.Application.Services;
using CampaignWatchWorker.Application.Services.Interfaces;
using CampaignWatchWorker.Application.Validators;
using Microsoft.Extensions.DependencyInjection;

namespace CampaignWatchWorker.Application.Resolver
{
    public static class ResolverIoC
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // Processors
            services.AddTransient<IProcessorApplication, ProcessorApplication>();

            // Mappers
            services.AddTransient<ICampaignMapper, CampaignMapper>();

            // Health Analyzers
            services.AddTransient<ICampaignHealthAnalyzer, CampaignHealthAnalyzer>();

            services.AddTransient<IAlertService, AlertService>();

            services.AddTransient<IEmailDispatcher, LogEmailDispatcher>();
            services.AddTransient<IWebhookDispatcher, LogWebhookDispatcher>();

            // Step Validators
            services.AddTransient<IStepValidator, FilterStepValidator>();
            services.AddTransient<IStepValidator, DecisionSplitStepValidator>();
            services.AddTransient<IStepValidator, ChannelStepValidator>();
            services.AddTransient<IStepValidator, WaitStepValidator>();
            services.AddTransient<IStepValidator, EndStepValidator>();
            services.AddTransient<IStepValidator, DatedStepValidator>();

            return services;
        }
    }
}
