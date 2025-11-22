using CampaignWatchWorker.Domain.Models.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CampaignWatchWorker.Domain.Models.Interfaces.Services.Scheduler
{
    public interface ISchedulerApiService
    {
        Task ScheduleExecutionAsync(ScheduleRequest request);
    }
}
