using CampaignWatchWorker.Domain.Models.Read.Campaign;

namespace CampaignWatchWorker.Domain.Models.Interfaces.Services.Read.Campaign
{
    public interface ICampaignReadModelService
    {
        /// <summary>
        /// Busca as campanhas de um cliente em sua respectiva base de dados de origem.
        /// </summary>
        /// <param name="dbName">O nome do banco de dados do cliente a ser consultado.</param>
        /// <returns>Uma coleção de campanhas lidas da fonte de dados.</returns>
        Task<IEnumerable<CampaignReadModel>> GetCampaigns(string dbName);
        /// <summary>
        /// Busca as execuções de uma campanha específica na base de dados de origem do cliente.
        /// </summary>
        /// <param name="dbName">O nome do banco de dados do cliente.</param>
        /// <param name="campaignId">O ID da campanha para a qual as execuções serão buscadas.</param>
        /// <returns>Uma coleção de execuções da campanha especificada.</returns>
        Task<IEnumerable<ExecutionReadModel>> GetExecutionsByCampaign(string dbName, string campaignId);
        /// <summary>
        /// Busca uma única campanha pelo seu ID na base de dados de origem do cliente.
        /// </summary>
        /// <param name="dbName">O nome do banco de dados do cliente.</param>
        /// <param name="campaignId">O ID da campanha a ser buscada.</param>
        /// <returns>A entidade da campanha lida da fonte de dados, ou nulo se não encontrada.</returns>
        Task<CampaignReadModel> GetCampaignById(string dbName, string campaignId);
    }
}
