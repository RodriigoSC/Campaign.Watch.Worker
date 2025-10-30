
using CampaignWatchWorker.Domain.Models.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CampaignWatchWorker.Domain.Models
{
    public class CampaignModel
    {
        [BsonId]
        public ObjectId Id { get; set; }

        public string ClientName { get; set; }
        public string IdCampaign { get; set; }
        public long NumberId { get; set; }
        public string Name { get; set; }
        public CampaignTypeEnum CampaignType { get; set; }
        public string Description { get; set; }
        public string ProjectId { get; set; }

        [BsonRepresentation(BsonType.Boolean)]
        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
        public CampaignStatusEnum StatusCampaign { get; set; }
        public MonitoringStatusEnum MonitoringStatus { get; set; }
        public DateTime? NextExecutionMonitoring { get; set; }
        public DateTime? LastCheckMonitoring { get; set; }
        public MonitoringHealthStatus HealthStatus { get; set; }

        [BsonRepresentation(BsonType.Boolean)]
        public bool IsDeleted { get; set; }

        [BsonRepresentation(BsonType.Boolean)]
        public bool IsRestored { get; set; }

        public Scheduler Scheduler { get; set; }

        public int TotalExecutionsProcessed { get; set; }
        public int ExecutionsWithErrors { get; set; }
        public DateTime? FirstMonitoringAt { get; set; }
        public string MonitoringNotes { get; set; }
    }
}