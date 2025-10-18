using CampaignWatchWorker.Domain.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CampaignWatchWorker.Domain.Models.Diagnostics
{
    public class ExecutionDiagnostic
    {
        public string ExecutionId { get; set; }
        public HealthStatusEnum OverallHealth { get; set; }
        public List<StepDiagnostic> StepDiagnostics { get; set; } = new();
        public DateTime AnalyzedAt { get; set; }
        public string Summary { get; set; }
    }
}
