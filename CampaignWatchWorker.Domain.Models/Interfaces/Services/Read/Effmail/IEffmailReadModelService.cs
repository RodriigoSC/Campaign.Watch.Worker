using CampaignWatchWorker.Domain.Models.Read.Effmail;

namespace CampaignWatchWorker.Domain.Models.Interfaces.Services.Read.Effmail
{
    public interface IEffmailReadModelService
    {
        /// <summary>
        /// Busca os dados da trigger do Effmail associados a um workflow específico.
        /// </summary>
        /// <param name="dbName">O nome do banco de dados do cliente a ser consultado.</param>
        /// <param name="workflowId">O ID do workflow para o qual os dados da trigger serão buscados.</param>
        /// <returns>Uma coleção de dados de Effmail lidos da fonte de dados.</returns>
        Task<IEnumerable<EffmailReadModel>> GetTriggerEffmail(string dbName, string workflowId);
    }
}
