using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CampaignWatchWorker.Domain.Models.Configuration
{
    public class ScheduleRequest
    {
        public string ClientName { get; set; }
        public string ProjectId { get; set; }
        public string CampaignId { get; set; }
        public DateTime ExecuteAt { get; set; }
    }
}
