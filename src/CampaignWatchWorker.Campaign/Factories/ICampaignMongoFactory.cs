using MongoDB.Driver;

namespace CampaignWatchWorker.Infra.Campaign.Factories
{
    public interface ICampaignMongoFactory
    {
        /// <summary>
        /// Obtém a instância do banco de dados de campanha de um cliente específico.
        /// </summary>
        /// <param name="dbName">O nome do banco de dados do cliente a ser acessado.</param>
        /// <returns>Uma instância de IMongoDatabase representando o banco de dados de campanha.</returns>
        IMongoDatabase GetDatabase(string dbName);
    }
}
