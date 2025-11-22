using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CampaignWatchWorker.Domain.Models.Configuration
{
    public class WorkerSettings
    {
        public string QueueName { get; set; }
        public int PrefetchCount { get; set; } = 5;
    }
}
