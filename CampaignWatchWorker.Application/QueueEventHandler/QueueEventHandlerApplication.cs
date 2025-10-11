using CampaignWatchWorker.Application.Services.Interfaces;

namespace CampaignWatchWorker.Application.QueueEventHandler
{
    public class QueueEventHandlerApplication : IQueueEventHandlerApplication
    {
        private readonly IQueueEventHandler _queueEventHandler;
        public QueueEventHandlerApplication(IQueueEventHandler queueEventHandler)
        {
            _queueEventHandler = queueEventHandler;
        }

        public void Get(Action<object, object?> action)
        {
            _queueEventHandler.Get(action);
        }
    }
}
