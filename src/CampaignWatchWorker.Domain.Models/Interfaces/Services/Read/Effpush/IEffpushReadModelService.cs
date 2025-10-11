using CampaignWatchWorker.Domain.Models.Read.Effpush;

namespace CampaignWatchWorker.Domain.Models.Interfaces.Services.Read.Effpush
{
    public interface IEffpushReadModelService
    {
        Task<IEnumerable<EffpushReadModel>> GetTriggerEffpush(string workflowId);
    }
}
