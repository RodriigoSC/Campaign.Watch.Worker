namespace CampaignWatchWorker.Domain.Models.Interfaces.Repositories
{
    public interface IExecutionModelRepository
    {
        Task<ExecutionModel> CriarExecucaoAsync(ExecutionModel executionModel);
        Task<bool> AtualizarExecucaoAsync(ExecutionModel executionModel);
    }
}
