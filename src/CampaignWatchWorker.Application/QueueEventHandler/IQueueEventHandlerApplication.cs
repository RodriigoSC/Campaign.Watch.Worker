using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CampaignWatchWorker.Application.QueueEventHandler
{
    public interface IQueueEventHandlerApplication
    {
        void Get(Action<object?, ProcessorEvent?> action);
    }

}
