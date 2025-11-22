using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CampaignWatchWorker.Application.QueueEventHandler
{
    public class QueueEventHandlerApplication : IQueueEventHandlerApplication
    {
        private readonly IQueueEventHandler _queueEventHandler;

        public QueueEventHandlerApplication(IQueueEventHandler queueEventHandler)
        {
            _queueEventHandler = queueEventHandler;
        }

        public void Get(Action<object?, ProcessorEvent?> action)
        {
            _queueEventHandler.Get(action);
        }
    }

}
