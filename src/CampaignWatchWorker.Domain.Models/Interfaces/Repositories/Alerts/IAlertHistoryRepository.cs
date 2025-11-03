using CampaignWatchWorker.Domain.Models.Entities.Alerts;

namespace CampaignWatchWorker.Domain.Models.Interfaces.Repositories.Alerts
{
    public interface IAlertHistoryRepository
    {
        Task<AlertHistoryModel> CreateAsync(AlertHistoryModel entity);
    }
}
