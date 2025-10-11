namespace CampaignWatchWorker.Application.Processor
{
    public class ProcessorApplication : IProcessorApplication
    {
        public void Process(object obj)
        {
            Console.WriteLine("Cheguei");
        }
    }
}
