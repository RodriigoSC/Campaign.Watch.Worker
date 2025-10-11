using CampaignWatchWorker.Domain.Models.Read.Effmail;

namespace CampaignWatchWorker.Domain.Models.Interfaces.Services.Read.Effmail
{
    public interface IEffmailReadModelService
    {
        Task<IEnumerable<EffmailReadModel>> GetTriggerEffmail(string workflowId);
    }
}
