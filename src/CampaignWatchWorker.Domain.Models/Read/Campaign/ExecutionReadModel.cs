using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace CampaignWatchWorker.Domain.Models.Read.Campaign
{
    [BsonIgnoreExtraElements]
    public class ExecutionReadModel
    {
        [JsonPropertyName("_id")]
        public ObjectId Id { get; set; }

        [JsonPropertyName("ProjectId")]
        public ObjectId ProjectId { get; set; }

        [JsonPropertyName("CampaignId")]
        public ObjectId CampaignId { get; set; }

        [JsonPropertyName("CampaignName")]
        public string CampaignName { get; set; }

        [JsonPropertyName("ExecutionId")]
        public ObjectId ExecutionId { get; set; }

        [JsonPropertyName("LastExecutionId")]
        public ObjectId LastExecutionId { get; set; }

        [JsonPropertyName("StartDate")]
        public DateTime StartDate { get; set; }

        [JsonPropertyName("EndDate")]
        public object EndDate { get; set; }

        [JsonPropertyName("Status")]
        public string Status { get; set; }

        [JsonPropertyName("IsCanceled")]
        [BsonRepresentation(BsonType.Boolean)]
        public bool IsCanceled { get; set; }

        [JsonPropertyName("QueryExternal")]
        public string QueryExternal { get; set; }

        [JsonPropertyName("FlagCount")]
        [BsonRepresentation(BsonType.Boolean)]
        public bool FlagCount { get; set; }

        [JsonPropertyName("WorkflowExecution")]
        public List<WorkflowExecutionReadModel> WorkflowExecution { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class WorkflowExecutionReadModel
    {
        [JsonPropertyName("_id")]
        public ObjectId Id { get; set; }

        [JsonPropertyName("Name")]
        public string Name { get; set; }

        [JsonPropertyName("Type")]
        public string Type { get; set; }

        [JsonPropertyName("Status")]
        public string Status { get; set; }

        [JsonPropertyName("StartDate")]
        public DateTime StartDate { get; set; }

        [JsonPropertyName("TotalUsers")]
        public int TotalUsers { get; set; }

        [JsonPropertyName("TotalExecutionTime")]
        public int TotalExecutionTime { get; set; }

        [JsonPropertyName("Error")]
        public BsonDocument Error { get; set; }

        [BsonElement("ExecutionData")]
        public BsonDocument ExecutionData { get; set; }
    }
}
