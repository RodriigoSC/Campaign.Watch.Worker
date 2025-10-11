using CampaignWatchWorker.Data.Factories.Common;
using DTM_Vault.Data.KeyValue;
using MongoDB.Driver;

namespace CampaignWatchWorker.Data.Factories
{
    public class PersistenceMongoFactory : IPersistenceMongoFactory
    {
        /// <summary>
        /// A fábrica genérica de MongoDB usada para criar a conexão real.
        /// </summary>
        private readonly IMongoDbFactory _factory;

        /// <summary>
        /// O serviço para buscar segredos (como o nome do banco de dados) do Vault.
        /// </summary>
        private readonly IVaultService _vault;

        /// <summary>
        /// O ambiente de execução atual (ex: "development", "production").
        /// </summary>
        private readonly string _environment;

        /// <summary>
        /// Inicializa uma nova instância da classe PersistenceMongoFactory.
        /// </summary>
        /// <param name="factory">A fábrica genérica de MongoDB a ser injetada.</param>
        /// <param name="vault">O serviço do Vault a ser injetado.</param>
        /// <param name="environment">O nome do ambiente atual.</param>
        public PersistenceMongoFactory(IMongoDbFactory factory, IVaultService vault, string environment)
        {
            _factory = factory;
            _vault = vault;
            _environment = environment;
        }

        /// <summary>
        /// Obtém a instância do banco de dados principal de persistência.
        /// </summary>
        /// <returns>Uma instância de IMongoDatabase representando o banco de dados da aplicação.</returns>
        public IMongoDatabase GetDatabase()
        {
            var dbName = _vault.GetKeyAsync($"monitoring/{_environment}/data/keys", "MongoDB.Persistence.database")
                                  .GetAwaiter().GetResult();

            return _factory.GetDatabase("MongoDB.Persistence", dbName);
        }
    }
}
