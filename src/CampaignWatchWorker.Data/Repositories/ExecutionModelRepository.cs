using CampaignWatchWorker.Data.Factories;
using CampaignWatchWorker.Data.Repositories.Common;
using CampaignWatchWorker.Domain.Models;
using CampaignWatchWorker.Domain.Models.Interfaces.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;

namespace CampaignWatchWorker.Data.Repositories
{
    public class ExecutionModelRepository : CommonRepository<ExecutionModel>, IExecutionModelRepository
    {
        public ExecutionModelRepository(IPersistenceMongoFactory persistenceFactory) : base(persistenceFactory, "ExecutionMonitoring")
        {
            var uniqueIndexKeys = Builders<ExecutionModel>.IndexKeys
                .Ascending(x => x.OriginalCampaignId)
                .Ascending(x => x.OriginalExecutionId);
            var uniqueIndexModel = new CreateIndexModel<ExecutionModel>(
                uniqueIndexKeys,
                new CreateIndexOptions { Unique = true, Name = "OriginalCampaign_OriginalExecution_Unique" });

            var queryIndexKeys = Builders<ExecutionModel>.IndexKeys
                .Ascending(x => x.CampaignMonitoringId);
            var queryIndexModel = new CreateIndexModel<ExecutionModel>(
                queryIndexKeys,
                new CreateIndexOptions { Name = "CampaignMonitoringId_Query" });

            CreateIndexesAsync(new List<CreateIndexModel<ExecutionModel>> { uniqueIndexModel, queryIndexModel }).GetAwaiter().GetResult();
        }

        public async Task<ExecutionModel> CreateExecutionAsync(ExecutionModel executionModel)
        {
            await _collection.InsertOneAsync(executionModel);
            return executionModel;
        }      

        public async Task<bool> UpdateExecutionAsync(ExecutionModel executionModel)
        {
            var filter = Builders<ExecutionModel>.Filter.And(
                Builders<ExecutionModel>.Filter.Eq(e => e.OriginalCampaignId, executionModel.OriginalCampaignId),
                Builders<ExecutionModel>.Filter.Eq(e => e.OriginalExecutionId, executionModel.OriginalExecutionId)
            );

            var updateDefinition = Builders<ExecutionModel>.Update
                .Set(x => x.CampaignName, executionModel.CampaignName)
                .Set(x => x.Status, executionModel.Status)
                .Set(x => x.StartDate, executionModel.StartDate)
                .Set(x => x.EndDate, executionModel.EndDate)
                .Set(x => x.TotalDurationInSeconds, executionModel.TotalDurationInSeconds)
                .Set(x => x.HasMonitoringErrors, executionModel.HasMonitoringErrors)
                .Set(x => x.Steps, executionModel.Steps)

                .SetOnInsert(x => x.Id, ObjectId.GenerateNewId())
                .SetOnInsert(x => x.CampaignMonitoringId, executionModel.CampaignMonitoringId)
                .SetOnInsert(x => x.OriginalCampaignId, executionModel.OriginalCampaignId)
                .SetOnInsert(x => x.OriginalExecutionId, executionModel.OriginalExecutionId);

            var options = new UpdateOptions { IsUpsert = true };

            var result = await _collection.UpdateOneAsync(filter, updateDefinition, options);

            return result.IsAcknowledged && (result.ModifiedCount > 0 || result.UpsertedId != null);
        }
    }
}
