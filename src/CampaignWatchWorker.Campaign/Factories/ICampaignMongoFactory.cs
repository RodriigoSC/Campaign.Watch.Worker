using MongoDB.Driver;

namespace CampaignWatchWorker.Infra.Campaign.Factories
{
    public interface ICampaignMongoFactory
    {
        IMongoDatabase GetDatabase();
    }
}
