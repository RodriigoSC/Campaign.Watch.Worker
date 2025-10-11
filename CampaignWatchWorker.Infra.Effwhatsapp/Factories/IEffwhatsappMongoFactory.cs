using MongoDB.Driver;

namespace CampaignWatchWorker.Infra.Effwhatsapp.Factories
{
    public interface IEffwhatsappMongoFactory
    {
        IMongoDatabase GetDatabase(string dbName);
    }
}
