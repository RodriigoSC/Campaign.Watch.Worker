using CampaignWatchWorker.Domain.Models.Enums;

namespace CampaignWatchWorker.Domain.Models.Diagnostics
{
    public class StepDiagnostic
    {
        public string StepId { get; set; }
        public string StepName { get; set; }
        public DiagnosticTypeEnum DiagnosticType { get; set; }
        public HealthStatusEnum Severity { get; set; }
        public string Message { get; set; }
        public DateTime DetectedAt { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; } = new();
    }
}
