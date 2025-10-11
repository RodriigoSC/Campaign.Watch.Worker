using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CampaignWatchWorker.Domain.Models.Read.Effwhatsapp
{
    [BsonIgnoreExtraElements]
    public class EffwhatsappReadModel
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("Status")]
        public string Status { get; set; }

        [BsonElement("Name")]
        public string Name { get; set; }

        [BsonElement("Parameters")]
        public Parameters Parameters { get; set; }

        [BsonElement("CreatedAt")]
        public DateTime? CreatedAt { get; set; }

        [BsonElement("ModifiedAt")]
        public DateTime? ModifiedAt { get; set; }

        [BsonElement("TemplateId")]
        public string TemplateId { get; set; }

        [BsonElement("Archive")]
        public ArchiveInfo Archive { get; set; }

        [BsonElement("BrokerType")]
        public int BrokerType { get; set; }

        [BsonElement("Error")]
        public string Error { get; set; }

        [BsonElement("Leads")]
        public Leads Leads { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class Parameters
    {
        [BsonElement("CampaignId")]
        public string CampaignId { get; set; }

        [BsonElement("ExecutionId")]
        public string ExecutionId { get; set; }

        [BsonElement("WorkflowId")]
        public string WorkflowId { get; set; }

        [BsonElement("ProjectId")]
        public string ProjectId { get; set; }

        [BsonElement("WorkflowName")]
        public string WorkflowName { get; set; }

        [BsonElement("NumberId")]
        public string NumberId { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class ArchiveInfo
    {
        [BsonElement("Name")]
        public string Name { get; set; }

        [BsonElement("Hash")]
        public string Hash { get; set; }

        [BsonElement("Separator")]
        public string Separator { get; set; }

        [BsonElement("Personalizations")]
        public string Personalizations { get; set; }

        [BsonElement("CreatedAt")]
        public DateTime? CreatedAt { get; set; }

        [BsonElement("StartedAt")]
        public DateTime? StartedAt { get; set; }

        [BsonElement("FinishedAt")]
        public DateTime? FinishedAt { get; set; }

        [BsonElement("Completed")]
        [BsonRepresentation(BsonType.Boolean)]
        public bool Completed { get; set; }

        [BsonElement("Total")]
        public long Total { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class Leads
    {
        [BsonElement("TriggerId")]
        public string TriggerId { get; set; }

        [BsonElement("Blocked")]
        public int? Blocked { get; set; }

        [BsonElement("Deduplication")]
        public int? Deduplication { get; set; }

        [BsonElement("Error")]
        public int? Error { get; set; }

        [BsonElement("Optout")]
        public int? Optout { get; set; }

        [BsonElement("Success")]
        public int? Success { get; set; }

        [BsonElement("Items")]
        public List<LeadDocument> Items { get; set; } = new List<LeadDocument>();
    }

    public class LeadDocument
    {
        [BsonElement("TriggerId")]
        public string TriggerId { get; set; }

        [BsonElement("Status")]
        public string Status { get; set; }
    }
}
