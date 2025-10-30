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
        public EffectiveChannels EffectiveChannels { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class CampaignDbConfig
    {
        public string ProjectID { get; set; }
        public string Database { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class EffectiveChannels
    {
        [BsonElement("EFFMAIL")]
        public List<ChannelDbConfig> Effmail { get; set; } = new();

        [BsonElement("EFFSMS")]
        public List<ChannelDbConfig> Effsms { get; set; } = new();

        [BsonElement("EFFPUSH")]
        public List<ChannelDbConfig> Effpush { get; set; } = new();

        [BsonElement("EFFWHATSAPP")]
        public List<ChannelDbConfig> Effwhatsapp { get; set; } = new();
    }

    [BsonIgnoreExtraElements]
    public class ChannelDbConfig
    {
        public string Name { get; set; }
        public string Integration { get; set; }
        public string DataBase { get; set; }
    }
}
