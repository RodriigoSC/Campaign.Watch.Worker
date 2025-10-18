using CampaignWatchWorker.Domain.Models;
using CampaignWatchWorker.Domain.Models.Diagnostics;

namespace CampaignWatchWorker.Application.Analyzer
{
    public interface ICampaignHealthAnalyzer
    {
        Task<ExecutionDiagnostic> AnalyzeExecutionAsync(ExecutionModel execution, CampaignModel campaign);
        Task<MonitoringHealthStatus> AnalyzeCampaignHealthAsync(CampaignModel campaign, List<ExecutionModel> executions);
    }
}
