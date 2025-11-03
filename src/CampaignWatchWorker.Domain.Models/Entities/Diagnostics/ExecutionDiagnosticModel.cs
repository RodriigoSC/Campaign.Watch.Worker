using CampaignWatchWorker.Domain.Models.Enums;

namespace CampaignWatchWorker.Domain.Models.Entities.Diagnostics
{
    public class ExecutionDiagnosticModel
    {
        public string ExecutionId { get; set; }
        public HealthStatusEnum OverallHealth { get; set; }
        public List<StepDiagnosticModel> StepDiagnostics { get; set; } = new();
        public DateTime AnalyzedAt { get; set; }
        public string Summary { get; set; }
    }
}
