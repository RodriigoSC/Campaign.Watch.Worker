using CampaignWatchWorker.Application.Processor;
using CampaignWatchWorker.Application.QueueEventHandler;

namespace CampaignWatchWorker.Worker
{
    public class Consumer
    {
        private readonly IQueueEventHandlerApplication _queueEventHandlerApplication;
        private readonly IProcessorApplication _processorApplication;
        public Consumer(IQueueEventHandlerApplication queueEventHandlerApplication,
            IProcessorApplication processorApplication)
        {
            _queueEventHandlerApplication = queueEventHandlerApplication;
            _processorApplication = processorApplication;
        }
        public void Start()
        {
            _queueEventHandlerApplication.Get(
                (model, obj) => _processorApplication.Process(obj));
        }


    }
}
