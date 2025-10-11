using MongoDB.Driver;

namespace CampaignWatchWorker.Infra.Effpush.Factories
{
    public interface IEffpushMongoFactory
    {
        IMongoDatabase GetDatabase(string dbName);
    }
}
