using CampaignWatchWorker.Domain.Models;
using CampaignWatchWorker.Domain.Models.Enums;
using CampaignWatchWorker.Domain.Models.Interfaces;
using CampaignWatchWorker.Domain.Models.Interfaces.Services.Read.Effmail;
using CampaignWatchWorker.Domain.Models.Interfaces.Services.Read.Effpush;
using CampaignWatchWorker.Domain.Models.Interfaces.Services.Read.Effsms;
using CampaignWatchWorker.Domain.Models.Interfaces.Services.Read.Effwhatsapp;
using CampaignWatchWorker.Domain.Models.Read.Campaign;
using MongoDB.Bson;
using Newtonsoft.Json;

namespace CampaignWatchWorker.Application.Mappers
{
    public class CampaignMapper : ICampaignMapper
    {
        private readonly ITenant _tenant;
        private readonly Dictionary<ChannelTypeEnum, Func<string, Task<ChannelIntegrationData>>> _channelFetchers;

        public CampaignMapper(
            ITenant tenant,
            IEffmailReadModelService effmailReadModelService,
            IEffsmsReadModelService effsmsReadModelService,
            IEffpushReadModelService effpushReadModelService,
            IEffwhatsappReadModelService effwhatsappReadModelService)
        {
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));

            _channelFetchers = new Dictionary<ChannelTypeEnum, Func<string, Task<ChannelIntegrationData>>>
            {
                [ChannelTypeEnum.EffectiveMail] = workflowId => FetchEffmailDataAsync(workflowId, effmailReadModelService),
                [ChannelTypeEnum.EffectiveSms] = workflowId => FetchEffsmsDataAsync(workflowId, effsmsReadModelService),
                [ChannelTypeEnum.EffectivePush] = workflowId => FetchEffpushDataAsync(workflowId, effpushReadModelService),
                [ChannelTypeEnum.EffectiveWhatsApp] = workflowId => FetchEffwhatsappDataAsync(workflowId, effwhatsappReadModelService)
            };
        }

        public CampaignModel MapToCampaignModel(CampaignReadModel campaignReadModel)
        {
            if (campaignReadModel == null) return null!;

            return new CampaignModel
            {
                ClientName = _tenant.Name,
                IdCampaign = campaignReadModel.Id,
                Name = campaignReadModel.Name,
                NumberId = campaignReadModel.NumberId,
                StatusCampaign = (CampaignStatusEnum)campaignReadModel.Status,
                CampaignType = (CampaignTypeEnum)campaignReadModel.Type,
                CreatedAt = campaignReadModel.CreatedAt,
                Description = campaignReadModel.Description,
                IsActive = campaignReadModel.IsActive,
                IsDeleted = campaignReadModel.IsDeleted,
                IsRestored = campaignReadModel.IsRestored,
                ModifiedAt = campaignReadModel.ModifiedAt,
                ProjectId = campaignReadModel.ProjectId,
                Scheduler = MapScheduler(campaignReadModel.Scheduler),
                HealthStatus = new MonitoringHealthStatus()
            };
        }

        public ExecutionModel MapToExecutionModel(ExecutionReadModel executionRead, ObjectId campaignMonitoringId)
        {
            if (executionRead == null) return null!;

            var executionModel = CreateExecutionModel(executionRead, campaignMonitoringId);
            executionModel.Steps = MapWorkflowSteps(executionRead.WorkflowExecution, executionRead.ExecutionId);
            /*executionModel.HasMonitoringErrors = executionModel.Steps.Any(s =>
                !string.IsNullOrEmpty(s.MonitoringNotes) || !string.IsNullOrEmpty(s.Error));*/

            return executionModel;
        }

        private ExecutionModel CreateExecutionModel(ExecutionReadModel executionRead, ObjectId campaignMonitoringId)
        {
            var endDate = executionRead.EndDate is DateTime dateTime ? dateTime : (DateTime?)null;
            var startDate = executionRead.StartDate;

            return new ExecutionModel
            {
                CampaignMonitoringId = campaignMonitoringId,
                OriginalCampaignId = executionRead.CampaignId.ToString(),
                OriginalExecutionId = executionRead.ExecutionId.ToString(),
                CampaignName = executionRead.CampaignName,
                Status = executionRead.Status,
                StartDate = startDate,
                EndDate = endDate,
                TotalDurationInSeconds = endDate.HasValue ? (endDate.Value - startDate).TotalSeconds : null,
                Steps = new List<WorkflowStep>(),
                HasMonitoringErrors = false
            };
        }

        private List<WorkflowStep> MapWorkflowSteps(IEnumerable<WorkflowExecutionReadModel> workflows, ObjectId executionId)
        {
            var steps = new List<WorkflowStep>();

            foreach (var workflow in workflows)
            {
                if (Enum.TryParse<WorkflowStepTypeEnum>(workflow.Type, true, out var stepType))
                {
                    var step = MapWorkflowStep(workflow, stepType);
                    steps.Add(step);
                }
            }

            return steps;
        }

        private WorkflowStep MapWorkflowStep(WorkflowExecutionReadModel workflow, WorkflowStepTypeEnum stepType)
        {
            var step = CreateBaseWorkflowStep(workflow, stepType);
            EnrichWorkflowStep(step, workflow, stepType);
            return step;
        }

        private WorkflowStep CreateBaseWorkflowStep(WorkflowExecutionReadModel workflow, WorkflowStepTypeEnum stepType)
        {
            return new WorkflowStep
            {
                OriginalStepId = workflow.Id.ToString(),
                Name = workflow.Name,
                Status = workflow.Status,
                TotalExecutionTime = workflow.TotalExecutionTime,
                TotalUser = workflow.TotalUsers,
                Type = stepType.ToString(),
                Error = workflow.Error?.ToString() ?? string.Empty,
            };
        }

        private void EnrichWorkflowStep(WorkflowStep step, WorkflowExecutionReadModel workflow, WorkflowStepTypeEnum stepType)
        {
            switch (stepType)
            {
                case WorkflowStepTypeEnum.Filter:
                    HandleFilterStep(step, workflow);
                    break;

                case WorkflowStepTypeEnum.Channel:
                    HandleChannelStep(step, workflow);
                    break;

                case WorkflowStepTypeEnum.End:
                    step.MonitoringNotes = "Etapa final da jornada.";
                    break;
            }
        }

        private void HandleFilterStep(WorkflowStep step, WorkflowExecutionReadModel workflow)
        {
            const int FilterTimeoutHours = 1;

            if (step.Status != "Completed" &&
                (DateTime.UtcNow - workflow.StartDate) > TimeSpan.FromHours(FilterTimeoutHours))
            {
                step.MonitoringNotes = $"ALERTA: Etapa de filtro está em execução há mais de {FilterTimeoutHours} hora(s).";
            }
        }

        private void HandleChannelStep(WorkflowStep step, WorkflowExecutionReadModel workflow)
        {
            if (!TryGetChannelType(workflow, out var channelType))
            {
                step.MonitoringNotes = "ERRO: Etapa do tipo Canal, mas não foi possível identificar o canal específico.";
                return;
            }

            step.ChannelName = channelType.ToString();
            step.IntegrationData = FetchChannelDataAsync(channelType, workflow.Id.ToString()).GetAwaiter().GetResult();
        }

        private bool TryGetChannelType(WorkflowExecutionReadModel workflow, out ChannelTypeEnum channelType)
        {
            channelType = default;

            return workflow.ExecutionData != null &&
                   workflow.ExecutionData.Contains("ChannelName") &&
                   Enum.TryParse(workflow.ExecutionData["ChannelName"].AsString, out channelType);
        }

        private async Task<ChannelIntegrationData> FetchChannelDataAsync(ChannelTypeEnum channelType, string workflowId)
        {
            if (_channelFetchers.TryGetValue(channelType, out var fetcher))
            {
                try
                {
                    return await fetcher(workflowId);
                }
                catch
                {
                    return null!;
                }
            }

            return null!;
        }

        private async Task<ChannelIntegrationData> FetchEffmailDataAsync(
            string workflowId,
            IEffmailReadModelService service)
        {
            var trigger = (await service.GetTriggerEffmail(workflowId)).FirstOrDefault();
            if (trigger == null) return null!;

            return new ChannelIntegrationData
            {
                ChannelName = ChannelTypeEnum.EffectiveMail.ToString(),
                IntegrationStatus = trigger.Status,
                TemplateId = trigger.TemplateId,
                Raw = JsonConvert.SerializeObject(trigger),
                Leads = MapLeadsData(trigger.Leads),
                File = MapFileInfoData(trigger.File)
            };
        }

        private async Task<ChannelIntegrationData> FetchEffsmsDataAsync(
            string workflowId,
            IEffsmsReadModelService service)
        {
            var trigger = (await service.GetTriggerEffsms(workflowId)).FirstOrDefault();
            if (trigger == null) return null!;

            return new ChannelIntegrationData
            {
                ChannelName = ChannelTypeEnum.EffectiveSms.ToString(),
                IntegrationStatus = trigger.Status,
                TemplateId = trigger.TemplateId,
                Raw = JsonConvert.SerializeObject(trigger),
                Leads = MapLeadsData(trigger.Leads),
                File = MapFileInfoData(trigger.File)
            };
        }

        private async Task<ChannelIntegrationData> FetchEffpushDataAsync(
            string workflowId,
            IEffpushReadModelService service)
        {
            var trigger = (await service.GetTriggerEffpush(workflowId)).FirstOrDefault();
            if (trigger == null) return null!;

            return new ChannelIntegrationData
            {
                ChannelName = ChannelTypeEnum.EffectivePush.ToString(),
                IntegrationStatus = trigger.Status,
                TemplateId = trigger.TemplateId,
                Raw = JsonConvert.SerializeObject(trigger),
                Leads = MapLeadsData(trigger.Leads),
                File = MapFileInfoData(trigger.File)
            };
        }

        private async Task<ChannelIntegrationData> FetchEffwhatsappDataAsync(
            string workflowId,
            IEffwhatsappReadModelService service)
        {
            var trigger = (await service.GetTriggerEffwhatsapp(workflowId)).FirstOrDefault();
            if (trigger == null) return null!;

            return new ChannelIntegrationData
            {
                ChannelName = ChannelTypeEnum.EffectiveWhatsApp.ToString(),
                IntegrationStatus = trigger.Status,
                TemplateId = trigger.TemplateId,
                Raw = JsonConvert.SerializeObject(trigger),
                Leads = MapLeadsData(trigger.Leads),
                File = MapFileInfoData(trigger.Archive)
            };
        }

        private Domain.Models.Scheduler MapScheduler(SchedulerReadModel scheduler)
        {
            if (scheduler == null) return null!;

            return new Domain.Models.Scheduler
            {
                Crontab = scheduler.Crontab,
                EndDateTime = scheduler.EndDateTime,
                IsRecurrent = scheduler.IsRecurrent,
                StartDateTime = scheduler.StartDateTime
            };
        }

        private LeadsData MapLeadsData<T>(T leads) where T : class
        {
            if (leads == null) return null!;

            var type = typeof(T);
            return new LeadsData
            {
                Success = (int)type.GetProperty("Success")?.GetValue(leads)!,
                Error = (int)type.GetProperty("Error")?.GetValue(leads)!,
                Blocked = (int)type.GetProperty("Blocked")?.GetValue(leads)!,
                Optout = (int)type.GetProperty("Optout")?.GetValue(leads)!,
                Deduplication = (int)type.GetProperty("Deduplication")?.GetValue(leads)!
            };
        }

        private FileInfoData MapFileInfoData<T>(T fileInfo) where T : class
        {
            if (fileInfo == null) return null!;

            var type = typeof(T);
            return new FileInfoData
            {
                Name = type.GetProperty("Name")?.GetValue(fileInfo) as string,
                StartedAt = (DateTime?)type.GetProperty("StartedAt")?.GetValue(fileInfo),
                FinishedAt = (DateTime?)type.GetProperty("FinishedAt")?.GetValue(fileInfo),
                Total = (long)type.GetProperty("Total")?.GetValue(fileInfo)!
            };
        }
    }
}