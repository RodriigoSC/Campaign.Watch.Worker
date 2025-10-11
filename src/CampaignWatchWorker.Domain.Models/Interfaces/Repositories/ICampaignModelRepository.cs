using MongoDB.Bson;

namespace CampaignWatchWorker.Domain.Models.Interfaces.Repositories
{
    public interface ICampaignModelRepository
    {
        Task<CampaignModel> CriarCampanhaAsync(CampaignModel campaignModel);
        Task<bool> AtualizarCampanhaAsync(CampaignModel campaignModel);
    }
}
