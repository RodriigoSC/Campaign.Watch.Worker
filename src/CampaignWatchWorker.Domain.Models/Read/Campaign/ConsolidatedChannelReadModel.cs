using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CampaignWatchWorker.Domain.Models.Read.Campaign
{
    [BsonIgnoreExtraElements]
    public class ConsolidatedChannelReadModel
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string Channel { get; set; }
        public string StatusTrigger { get; set; }
        public string CampaignId { get; set; }
        public string ExecutionId { get; set; }
        public string WorkflowId { get; set; }

        [BsonElement("TotalStatus")]
        public ChannelTotalStatus TotalStatus { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class ChannelTotalStatus
    {
        public int? TotalDeduplication { get; set; }
        public int? TotalBlocked { get; set; }
        public int? TotalUnknown { get; set; }

        public int? TotalSuccess { get; set; }
        public int? TotalOptout { get; set; }
        public int? TotalError { get; set; }

        public int? TotalFail { get; set; }
    }
}
