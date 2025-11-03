using CampaignWatchWorker.Domain.Models.Enums.Alerts;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CampaignWatchWorker.Domain.Models.Entities.Alerts
{
    public class AlertConfigurationModel
    {
        [BsonId]
        public ObjectId Id { get; set; }
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId? ClientId { get; set; }

        public string Name { get; set; }

        [BsonRepresentation(BsonType.String)]
        public AlertChannelType Type { get; set; }

        [BsonRepresentation(BsonType.String)]
        public AlertConditionType? ConditionType { get; set; }

        [BsonRepresentation(BsonType.String)]
        public AlertSeverity? MinSeverity { get; set; }

        public string Recipient { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
