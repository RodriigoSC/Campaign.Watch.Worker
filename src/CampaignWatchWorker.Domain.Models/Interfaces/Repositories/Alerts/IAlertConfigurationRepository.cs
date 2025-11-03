using CampaignWatchWorker.Domain.Models.Entities.Alerts;
using MongoDB.Bson;

namespace CampaignWatchWorker.Domain.Models.Interfaces.Repositories.Alerts
{
    public interface IAlertConfigurationRepository
    {
        Task<IEnumerable<AlertConfigurationModel>> GetByScopeAsync(ObjectId? clientId);
    }
}
