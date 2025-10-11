namespace CampaignWatchWorker.Application.QueueEventHandler
{
    public interface IQueueEventHandlerApplication
    {
        void Get(Action<object, object?> action);
    }
}
