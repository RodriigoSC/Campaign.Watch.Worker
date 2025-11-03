namespace CampaignWatchWorker.Domain.Models.Enums.Alerts
{
    public enum AlertChannelType
    {
        Email,
        Webhook
    }

    public enum AlertConditionType
    {
        StepFailed,
        ExecutionDelayed,
        FilterStuck,
        IntegrationError,
        CampaignNotFinalized
    }

    public enum AlertSeverity
    {
        Healthy,
        Warning,
        Error,
        Critical
    }
}
