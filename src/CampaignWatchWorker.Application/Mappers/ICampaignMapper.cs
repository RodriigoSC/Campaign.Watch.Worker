
using CampaignWatchWorker.Domain.Models;
using CampaignWatchWorker.Domain.Models.Read.Campaign;
using MongoDB.Bson;

namespace CampaignWatchWorker.Application.Mappers
{
    public interface ICampaignMapper
    {
        /// <summary>
        /// Mapeia o modelo de leitura de campanha para o modelo de domínio de monitoramento.
        /// </summary>
        CampaignModel MapToCampaignModel(CampaignReadModel campaignReadModel);

        /// <summary>
        /// Mapeia o modelo de leitura de execução para o modelo de domínio de monitoramento.
        /// </summary>
        ExecutionModel MapToExecutionModel(ExecutionReadModel executionReadModel, ObjectId campaignMonitoringId);
    }
}
