﻿using CampaignWatchWorker.Domain.Models.Interfaces.Services.Read.Campaign;
using CampaignWatchWorker.Domain.Models.Read.Campaign;
using CampaignWatchWorker.Infra.Campaign.Factories;
using MongoDB.Bson;
using MongoDB.Driver;

namespace CampaignWatchWorker.Infra.Campaign.Services
{
    public class CampaignReadModelService : ICampaignReadModelService
    {
        private readonly ICampaignMongoFactory _factory;
        
        public CampaignReadModelService(ICampaignMongoFactory factory)
        {
            _factory = factory;
        }

        public async Task<IEnumerable<CampaignReadModel>> GetCampaigns()
        {
            var db = _factory.GetDatabase();
            var collection = db.GetCollection<CampaignReadModel>("Campaign");
            return await collection.Find(_ => true).ToListAsync();
        }

        public async Task<IEnumerable<ExecutionReadModel>> GetExecutionsByCampaign(string campaignId)
        {
            var db = _factory.GetDatabase();
            var collection = db.GetCollection<ExecutionReadModel>("ExecutionPlan");

            return await collection
                .Find(x => x.CampaignId.ToString() == campaignId && x.FlagCount == false)
                .ToListAsync();
        }

        public async Task<CampaignReadModel> GetCampaignById(string campaignId)
        {
            var db = _factory.GetDatabase();
            var collection = db.GetCollection<CampaignReadModel>("Campaign");

            if (!ObjectId.TryParse(campaignId, out var campaignObjectId))
            {
                return null;
            }

            return await collection.Find(x => x.Id == campaignObjectId.ToString()).FirstOrDefaultAsync();
        }
    }
}
