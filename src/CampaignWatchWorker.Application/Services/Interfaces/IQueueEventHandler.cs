namespace CampaignWatchWorker.Application.Services.Interfaces
{
    public interface IQueueEventHandler
    {
        void Get(Action<object?, object?> action);
    }
}
