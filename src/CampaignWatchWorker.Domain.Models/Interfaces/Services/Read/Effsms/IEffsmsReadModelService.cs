using CampaignWatchWorker.Domain.Models.Read.Effsms;

namespace CampaignWatchWorker.Domain.Models.Interfaces.Services.Read.Effsms
{
    public interface IEffsmsReadModelService
    {
        Task<IEnumerable<EffsmsReadModel>> GetTriggerEffsms(string workflowId);
    }
}
