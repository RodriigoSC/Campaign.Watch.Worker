using CampaignWatchWorker.Domain.Models.Interfaces.Services.Read.Campaign;
using CampaignWatchWorker.Domain.Models.Read.Campaign;
using CampaignWatchWorker.Infra.Campaign.Factories;
using MongoDB.Driver;

namespace CampaignWatchWorker.Infra.Campaign.Services
{
    public class ChannelReadModelService : IChannelReadModelService
    {
        private readonly ICampaignMongoFactory _factory;
        private const string ConsolidatedCollectionName = "Deliverability";

        public ChannelReadModelService(ICampaignMongoFactory factory)
        {
            _factory = factory;
        }

        public async Task<Dictionary<string, ConsolidatedChannelReadModel>> GetConsolidatedChannelDataAsync(string executionId)
        {
            var db = _factory.GetDatabase();
            var collection = db.GetCollection<ConsolidatedChannelReadModel>(ConsolidatedCollectionName);

            var filter = Builders<ConsolidatedChannelReadModel>.Filter.Eq(x => x.ExecutionId, executionId);
            var results = await collection.Find(filter).ToListAsync();

            return results.ToDictionary(r => r.WorkflowId, r => r);
        }
    }
}
