using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CampaignWatchWorker.Domain.Models
{
    [BsonIgnoreExtraElements]
    public class ClientConfig
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public CampaignDbConfig CampaignConfig { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class CampaignDbConfig
    {
        public string ProjectID { get; set; }
        public string Database { get; set; }
    }    
}
