using CampaignWatchWorker.Data.Factories;
using MongoDB.Driver;

namespace CampaignWatchWorker.Data.Repositories.Common
{
    public abstract class CommonRepository<TEntity> where TEntity : class
    {
        protected readonly IMongoCollection<TEntity> _collection;

        protected CommonRepository(IPersistenceMongoFactory persistenceFactory, string collectionName)
        {
            var database = persistenceFactory.GetDatabase();
            _collection = database.GetCollection<TEntity>(collectionName);
        }

        protected async Task CreateIndexesAsync(IEnumerable<CreateIndexModel<TEntity>> indexes)
        {
            if (indexes != null && indexes.Any())
            {
                await _collection.Indexes.CreateManyAsync(indexes);
            }
        }
    }
}
