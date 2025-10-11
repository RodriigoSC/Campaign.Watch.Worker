using CampaignWatchWorker.Domain.Models.Read.Effwhatsapp;


namespace CampaignWatchWorker.Domain.Models.Interfaces.Services.Read.Effwhatsapp
{
    public interface IEffwhatsappReadModelService
    {
        Task<IEnumerable<EffwhatsappReadModel>> GetTriggerEffwhatsapp(string workflowId);
    }
}
