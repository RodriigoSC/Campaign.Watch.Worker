using CampaignWatchWorker.Domain.Models.Entities.Campaigns;
using CampaignWatchWorker.Domain.Models.Read.Campaign;
using MongoDB.Bson;

namespace CampaignWatchWorker.Application.Mappers
{
    public interface ICampaignMapper
    {
        CampaignModel MapToCampaignModel(CampaignReadModel campaignReadModel);

        ExecutionModel MapToExecutionModel(ExecutionReadModel executionReadModel, ObjectId campaignMonitoringId,
            Dictionary<string, ConsolidatedChannelReadModel> channelData);
    }
}
