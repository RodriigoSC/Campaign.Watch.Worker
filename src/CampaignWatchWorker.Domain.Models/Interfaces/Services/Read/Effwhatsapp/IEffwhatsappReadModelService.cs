using CampaignWatchWorker.Domain.Models.Read.Effwhatsapp;


namespace CampaignWatchWorker.Domain.Models.Interfaces.Services.Read.Effwhatsapp
{
    public interface IEffwhatsappReadModelService
    {
        /// <summary>
        /// Busca os dados da trigger do Effwhatsapp associados a um workflow específico,
        /// agregando as estatísticas dos leads.
        /// </summary>
        /// <param name="dbName">O nome do banco de dados do cliente a ser consultado.</param>
        /// <param name="workflowId">O ID do workflow para o qual os dados da trigger serão buscados.</param>
        /// <returns>Uma coleção de dados de Effwhatsapp lidos e agregados da fonte de dados.</returns>
        Task<IEnumerable<EffwhatsappReadModel>> GetTriggerEffwhatsapp(string dbName, string workflowId);
    }
}
