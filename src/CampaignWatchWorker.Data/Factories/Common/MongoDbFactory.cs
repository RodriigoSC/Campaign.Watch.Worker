using DTM_Vault.Data.KeyValue;
using MongoDB.Driver;
using System.Collections.Concurrent;

namespace CampaignWatchWorker.Data.Factories.Common
{
    public class MongoDbFactory : IMongoDbFactory
    {
        /// <summary>
        /// Serviço para buscar segredos (credenciais) do Vault.
        /// </summary>
        private readonly IVaultService _vaultService;

        /// <summary>
        /// O ambiente de execução (ex: "development", "production") para construir o caminho no Vault.
        /// </summary>
        private readonly string _environment;

        /// <summary>
        /// Dicionário concorrente para armazenar e reutilizar instâncias de MongoClient,
        /// usando a chave de conexão como identificador. Isso evita criar múltiplas conexões desnecessariamente.
        /// </summary>
        private readonly ConcurrentDictionary<string, MongoClient> _clients = new();

        /// <summary>
        /// Inicializa uma nova instância da classe MongoDbFactory.
        /// </summary>
        /// <param name="vaultService">A instância do serviço do Vault para ser injetada.</param>
        /// <param name="environment">O nome do ambiente atual.</param>
        public MongoDbFactory(IVaultService vaultService, string environment)
        {
            _vaultService = vaultService;
            _environment = environment;
        }

        /// <summary>
        /// Obtém ou cria uma instância de MongoClient para uma chave de conexão específica.
        /// Se um cliente para a chave já existir no cache, ele é retornado; caso contrário,
        /// as credenciais são buscadas no Vault e um novo cliente é criado e armazenado em cache.
        /// </summary>
        /// <param name="connectionKey">A chave que identifica a conexão (ex: "MongoDB.Campaign", "MongoDB.Effmail").</param>
        /// <returns>Uma instância de MongoClient.</returns>
        private MongoClient GetClient(string connectionKey)
        {
            return _clients.GetOrAdd(connectionKey, key =>
            {
                var host = _vaultService.GetKeyAsync($"monitoring/{_environment}/data/keys", $"{key}.host").GetAwaiter().GetResult();
                var user = _vaultService.GetKeyAsync($"monitoring/{_environment}/data/keys", $"{key}.user").GetAwaiter().GetResult();
                var pass = _vaultService.GetKeyAsync($"monitoring/{_environment}/data/keys", $"{key}.pass").GetAwaiter().GetResult();

                var connString = $"mongodb://{user}:{pass}@{host}";
                return new MongoClient(connString);
            });
        }

        /// <summary>
        /// Obtém uma instância do banco de dados MongoDB com base em uma chave de conexão e um nome de banco de dados.
        /// </summary>
        /// <param name="connectionKey">A chave usada para obter a string de conexão (ex: "MongoDB.Campaign").</param>
        /// <param name="databaseName">O nome do banco de dados ao qual se conectar.</param>
        /// <returns>Uma instância de IMongoDatabase configurada para o banco de dados especificado.</returns>
        public IMongoDatabase GetDatabase(string connectionKey, string databaseName)
        {
            var client = GetClient(connectionKey);
            return client.GetDatabase(databaseName);
        }
    }
}
