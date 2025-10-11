// Arquivo: ExecutionModel.cs

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace CampaignWatchWorker.Domain.Models
{
    /// <summary>
    /// Representa uma única execução de uma campanha.
    /// Cada instância desta classe é um documento na coleção "Executions".
    /// </summary>
    public class ExecutionModel
    {
        [BsonId]
        public ObjectId Id { get; set; } 

        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId CampaignId { get; set; }

        public string ExecutionId { get; set; }

        public string CampaignName { get; set; }

        public string Status { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public double StartLatencyInSeconds { get; set; }

        public double? TotalDurationInSeconds { get; set; }

        [BsonRepresentation(BsonType.Boolean)]
        public bool IsFullyVerifiedByMonitoring { get; set; }

        [BsonRepresentation(BsonType.Boolean)]
        public bool HasMonitoringErrors { get; set; }

        public List<WorkflowStep> Steps { get; set; } 
    }
}