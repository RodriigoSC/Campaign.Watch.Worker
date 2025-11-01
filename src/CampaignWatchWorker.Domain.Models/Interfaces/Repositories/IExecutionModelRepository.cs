namespace CampaignWatchWorker.Domain.Models.Interfaces.Repositories
{
    public interface IExecutionModelRepository
    {
        Task<ExecutionModel> CreateExecutionAsync(ExecutionModel executionModel);
        Task<bool> UpdateExecutionAsync(ExecutionModel executionModel);
    }
}
