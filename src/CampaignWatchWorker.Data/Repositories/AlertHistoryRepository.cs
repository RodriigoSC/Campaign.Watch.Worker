using CampaignWatchWorker.Data.Factories;
using CampaignWatchWorker.Data.Repositories.Common;
using CampaignWatchWorker.Domain.Models.Entities.Alerts;
using CampaignWatchWorker.Domain.Models.Interfaces.Repositories.Alerts;

namespace CampaignWatchWorker.Data.Repositories
{
    public class AlertHistoryRepository : CommonRepository<AlertHistoryModel>, IAlertHistoryRepository
    {
        public AlertHistoryRepository(IPersistenceMongoFactory persistenceFactory)
            : base(persistenceFactory, "AlertHistory")
        {
        }

        public async Task CreateAsync(AlertHistoryModel entity)
        {
            await _collection.InsertOneAsync(entity);
        }

        Task<AlertHistoryModel> IAlertHistoryRepository.CreateAsync(AlertHistoryModel entity)
        {
            throw new NotImplementedException();
        }
    }
}
