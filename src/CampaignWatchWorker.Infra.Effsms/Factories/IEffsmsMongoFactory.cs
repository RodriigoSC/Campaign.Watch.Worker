using MongoDB.Driver;

namespace CampaignWatchWorker.Infra.Effsms.Factories
{
    public interface IEffsmsMongoFactory
    {
        IMongoDatabase GetDatabase();
    }
}
