using MongoDB.Driver;

namespace CampaignWatchWorker.Data.Factories.Common
{
    public interface IMongoDbFactory
    {
        IMongoDatabase GetDatabase(string connectionKey, string databaseName);
    }
}
