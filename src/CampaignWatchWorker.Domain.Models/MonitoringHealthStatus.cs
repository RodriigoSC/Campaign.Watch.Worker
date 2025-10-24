﻿using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;


namespace CampaignWatchWorker.Domain.Models
{
    public class MonitoringHealthStatus
    {
        [BsonRepresentation(BsonType.Boolean)]
        public bool IsFullyVerified { get; set; }

        [BsonRepresentation(BsonType.Boolean)]
        public bool HasPendingExecution { get; set; }

        [BsonRepresentation(BsonType.Boolean)]
        public bool HasIntegrationErrors { get; set; }

        public string LastExecutionWithIssueId { get; set; }

        public string LastMessage { get; set; }
    }

    public class Scheduler
    {
        public DateTime StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }

        [BsonRepresentation(BsonType.Boolean)]
        public bool IsRecurrent { get; set; }
        public string Crontab { get; set; }
    }

    public class WorkflowStep
    {
        public string OriginalStepId { get; set; } // ID do Step no sistema de origem
        public string Name { get; set; }
        public string Type { get; set; } // Ex: Filter, Channel, End
        public string Status { get; set; }
        //public DateTime StartDate { get; set; }
        public long TotalUser { get; set; }
        public long TotalExecutionTime { get; set; }
        public string Error { get; set; } 
        public string ChannelName { get; set; } 
        public string MonitoringNotes { get; set; }
        public ChannelIntegrationData IntegrationData { get; set; }
    }

    public class ChannelIntegrationData
    {
        public string? Raw { get; set; }
        public string ChannelName { get; set; }
        public string IntegrationStatus { get; set; }
        public string TemplateId { get; set; }
        public FileInfoData File { get; set; }
        public LeadsData Leads { get; set; }
    }

    public class FileInfoData
    {
        public string Name { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public long Total { get; set; }
    }

    public class LeadsData
    {
        public int? Blocked { get; set; }
        public int? Deduplication { get; set; }
        public int? Error { get; set; }
        public int? Optout { get; set; }
        public int? Success { get; set; }
    }
}