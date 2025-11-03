using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CampaignWatchWorker.Domain.Models.Entities.Alerts
{
    public class AlertHistoryModel
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId? ClientId { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId AlertConfigurationId { get; set; }

        public string Severity { get; set; }

        public string Message { get; set; }

        public string CampaignName { get; set; }

        public string StepName { get; set; }

        public DateTime DetectedAt { get; set; }
    }
}
