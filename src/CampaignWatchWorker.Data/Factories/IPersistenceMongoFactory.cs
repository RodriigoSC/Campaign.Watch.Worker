using MongoDB.Driver;

namespace CampaignWatchWorker.Data.Factories
{
    public interface IPersistenceMongoFactory
    {
        /// <summary>
        /// Obtém a instância do banco de dados principal de persistência.
        /// </summary>
        /// <returns>Uma instância de IMongoDatabase representando o banco de dados da aplicação.</returns>
        IMongoDatabase GetDatabase();
    }
}
