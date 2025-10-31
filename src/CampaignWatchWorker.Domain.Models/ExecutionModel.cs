using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;


namespace CampaignWatchWorker.Domain.Models
{
    public class ExecutionModel
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId CampaignMonitoringId { get; set; }

        public string OriginalCampaignId { get; set; }

        public string OriginalExecutionId { get; set; }

        public string CampaignName { get; set; }

        public string Status { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public double? TotalDurationInSeconds { get; set; }

        [BsonRepresentation(BsonType.Boolean)]
        public bool HasMonitoringErrors { get; set; }

        public List<WorkflowStep> Steps { get; set; }
    }
}