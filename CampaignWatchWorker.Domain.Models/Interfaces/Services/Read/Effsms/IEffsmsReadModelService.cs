using CampaignWatchWorker.Domain.Models.Read.Effsms;

namespace CampaignWatchWorker.Domain.Models.Interfaces.Services.Read.Effsms
{
    public interface IEffsmsReadModelService
    {
        /// <summary>
        /// Busca os dados da trigger do Effsms associados a um workflow específico,
        /// agregando as estatísticas dos leads.
        /// </summary>
        /// <param name="dbName">O nome do banco de dados do cliente a ser consultado.</param>
        /// <param name="workflowId">O ID do workflow para o qual os dados da trigger serão buscados.</param>
        /// <returns>Uma coleção de dados de Effsms lidos e agregados da fonte de dados.</returns>
        Task<IEnumerable<EffsmsReadModel>> GetTriggerEffsms(string dbName, string workflowId);
    }
}
