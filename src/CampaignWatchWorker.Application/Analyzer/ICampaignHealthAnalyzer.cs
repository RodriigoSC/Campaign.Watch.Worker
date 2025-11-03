using CampaignWatchWorker.Domain.Models.Entities.Campaigns;
using CampaignWatchWorker.Domain.Models.Entities.Diagnostics;

namespace CampaignWatchWorker.Application.Analyzer
{
    public interface ICampaignHealthAnalyzer
    {
        Task<ExecutionDiagnosticModel> AnalyzeExecutionAsync(ExecutionModel execution, CampaignModel campaign);
        Task<MonitoringModel> AnalyzeCampaignHealthAsync(CampaignModel campaign, List<ExecutionModel> executions);
    }
}
