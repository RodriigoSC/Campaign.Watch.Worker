using CampaignWatchWorker.Domain.Models;
using CampaignWatchWorker.Domain.Models.Enums;
using CampaignWatchWorker.Domain.Models.Interfaces;
using CampaignWatchWorker.Domain.Models.Read.Campaign;
using MongoDB.Bson;
using Newtonsoft.Json;

namespace CampaignWatchWorker.Application.Mappers
{
    public class CampaignMapper : ICampaignMapper
    {
        private readonly ITenantContext _tenantContext;

        public CampaignMapper(ITenantContext tenantContext)
        {
            _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        }

        public CampaignModel MapToCampaignModel(CampaignReadModel campaignReadModel)
        {
            if (campaignReadModel == null) return null!;

            return new CampaignModel
            {
                ClientName = _tenantContext.Client.Name,
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

        public ExecutionModel MapToExecutionModel(ExecutionReadModel executionRead, ObjectId campaignMonitoringId, Dictionary<string, ConsolidatedChannelReadModel> channelData)
        {
            if (executionRead == null) return null!;

            var executionModel = CreateExecutionModel(executionRead, campaignMonitoringId);
            executionModel.Steps = MapWorkflowSteps(executionRead.WorkflowExecution, executionRead.ExecutionId, channelData);

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

        private List<WorkflowStep> MapWorkflowSteps(IEnumerable<WorkflowExecutionReadModel> workflows, ObjectId executionId, Dictionary<string, ConsolidatedChannelReadModel> channelData)
        {
            var steps = new List<WorkflowStep>();
            foreach (var workflow in workflows)
            {
                if (Enum.TryParse<WorkflowStepTypeEnum>(workflow.Type, true, out var stepType))
                {
                    var step = MapWorkflowStep(workflow, stepType, channelData);
                    steps.Add(step);
                }
            }
            return steps;
        }

        private WorkflowStep MapWorkflowStep(WorkflowExecutionReadModel workflow, WorkflowStepTypeEnum stepType, Dictionary<string, ConsolidatedChannelReadModel> channelData)
        {
            var step = CreateBaseWorkflowStep(workflow, stepType);
            EnrichWorkflowStep(step, workflow, stepType, channelData);
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

        private void EnrichWorkflowStep(WorkflowStep step, WorkflowExecutionReadModel workflow, WorkflowStepTypeEnum stepType, Dictionary<string, ConsolidatedChannelReadModel> channelData)
        {
            switch (stepType)
            {
                case WorkflowStepTypeEnum.Filter:
                    HandleFilterStep(step, workflow);
                    break;
                case WorkflowStepTypeEnum.Channel:
                    HandleChannelStep(step, workflow, channelData);
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

        private void HandleChannelStep(WorkflowStep step, WorkflowExecutionReadModel workflow, Dictionary<string, ConsolidatedChannelReadModel> channelData)
        {
            if (!TryGetChannelType(workflow, out var channelType))
            {
                step.MonitoringNotes = "ERRO: Etapa do tipo Canal, mas não foi possível identificar o canal específico.";
                return;
            }
            step.ChannelName = channelType.ToString();

            if (channelData.TryGetValue(step.OriginalStepId, out var consolidatedData))
            {
                step.IntegrationData = new ChannelIntegrationData
                {
                    ChannelName = consolidatedData.Channel,
                    IntegrationStatus = consolidatedData.StatusTrigger,
                    TemplateId = null,
                    Raw = JsonConvert.SerializeObject(consolidatedData),
                    Leads = MapLeadsData(consolidatedData.TotalStatus),
                    File = null
                };
            }
            else
            {
                step.MonitoringNotes = "AVISO: Dados de integração do canal não encontrados na coleção consolidada.";
            }
        }

        private bool TryGetChannelType(WorkflowExecutionReadModel workflow, out ChannelTypeEnum channelType)
        {
            channelType = default;

            return workflow.ExecutionData != null &&
                   workflow.ExecutionData.Contains("ChannelName") &&
                   Enum.TryParse(workflow.ExecutionData["ChannelName"].AsString, out channelType);
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

        private LeadsData MapLeadsData(ChannelTotalStatus totalStatus)
        {
            if (totalStatus == null) return null!;

            int totalError = (totalStatus.TotalError ?? 0) + (totalStatus.TotalFail ?? 0);

            return new LeadsData
            {
                Success = totalStatus.TotalSuccess ?? 0,
                Error = totalError,
                Blocked = totalStatus.TotalBlocked ?? 0,
                Optout = totalStatus.TotalOptout ?? 0,
                Deduplication = totalStatus.TotalDeduplication ?? 0
            };
        }
    }
}