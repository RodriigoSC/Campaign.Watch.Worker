using MongoDB.Driver;

namespace CampaignWatchWorker.Infra.Effmail.Factories
{
    public interface IEffmailMongoFactory
    {
        IMongoDatabase GetDatabase();
    }
}
