using CampaignWatchWorker.Application.Mappers;
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
        private readonly IEffmailReadModelService _effmailReadModelService;
        private readonly IEffsmsReadModelService _effsmsReadModelService;
        private readonly IEffpushReadModelService _effpushReadModelService;
        private readonly IEffwhatsappReadModelService _effwhatsappReadModelService;

        public CampaignMapper(
            ITenant tenant,
            IEffmailReadModelService effmailReadModelService,
            IEffsmsReadModelService effsmsReadModelService,
            IEffpushReadModelService effpushReadModelService,
            IEffwhatsappReadModelService effwhatsappReadModelService)
        {
            _tenant = tenant;
            _effmailReadModelService = effmailReadModelService;
            _effsmsReadModelService = effsmsReadModelService;
            _effpushReadModelService = effpushReadModelService;
            _effwhatsappReadModelService = effwhatsappReadModelService;
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
                Scheduler = new Domain.Models.Scheduler
                {
                    Crontab = campaignReadModel.Scheduler.Crontab,
                    EndDateTime = campaignReadModel.Scheduler.EndDateTime,
                    IsRecurrent = campaignReadModel.Scheduler.IsRecurrent,
                    StartDateTime = campaignReadModel.Scheduler.StartDateTime
                },
                HealthStatus = new MonitoringHealthStatus()
            };
        }

        public ExecutionModel MapToExecutionModel(ExecutionReadModel executionRead, ObjectId campaignMonitoringId)
        {
            if (executionRead == null) return null!;

            var executionModel = new ExecutionModel
            {
                CampaignMonitoringId = campaignMonitoringId,
                OriginalCampaignId = executionRead.CampaignId.ToString(),
                OriginalExecutionId = executionRead.ExecutionId.ToString(),
                CampaignName = executionRead.CampaignName,
                Status = executionRead.Status,
                StartDate = executionRead.StartDate,
                EndDate = (executionRead.EndDate is DateTime endDate) ? endDate : (DateTime?)null,
                Steps = new(),
                HasMonitoringErrors = false
            };

            executionModel.TotalDurationInSeconds = (executionModel.EndDate - executionModel.StartDate)?.TotalSeconds;

            foreach (var workflow in executionRead.WorkflowExecution)
            {
                if (Enum.TryParse<WorkflowStepTypeEnum>(workflow.Type, true, out var stepType))
                {
                    var step = MapWorkflowStep(workflow, stepType);
                    executionModel.Steps.Add(step);
                    if (!string.IsNullOrEmpty(step.MonitoringNotes) || !string.IsNullOrEmpty(step.Error))
                    {
                        executionModel.HasMonitoringErrors = true;
                    }
                }
                else
                {
                    Console.WriteLine($"AVISO: Tipo de step desconhecido: '{workflow.Type}' na execução {executionRead.ExecutionId}.");
                }
            }

            return executionModel;
        }

        private WorkflowStep MapWorkflowStep(WorkflowExecutionReadModel workflow, WorkflowStepTypeEnum stepType)
        {
            var step = new WorkflowStep
            {
                OriginalStepId = workflow.Id.ToString(),
                Name = workflow.Name,
                Status = workflow.Status,
                TotalExecutionTime = workflow.TotalExecutionTime,
                TotalUser = workflow.TotalUsers,
                Type = stepType.ToString(),
                Error = workflow.Error?.ToString() ?? string.Empty,
            };

            switch (stepType)
            {
                case WorkflowStepTypeEnum.Filter:
                    if (step.Status != "Completed" && (DateTime.UtcNow - workflow.StartDate) > TimeSpan.FromHours(1))
                    {
                        step.MonitoringNotes = "ALERTA: Etapa de filtro está em execução há mais de 1 hora.";
                    }
                    break;

                case WorkflowStepTypeEnum.Channel:
                    if (workflow.ExecutionData != null &&
                        workflow.ExecutionData.Contains("ChannelName") &&
                        Enum.TryParse<ChannelTypeEnum>(workflow.ExecutionData["ChannelName"].AsString, out var channelType))
                    {
                        step.ChannelName = channelType.ToString();
                        step.IntegrationData = FetchChannelData(channelType, workflow.Id.ToString());
                    }
                    else
                    {
                        step.MonitoringNotes = "ERRO: Etapa do tipo Canal, mas não foi possível identificar o canal específico.";
                    }
                    break;

                case WorkflowStepTypeEnum.End:
                    step.MonitoringNotes = "Etapa final da jornada.";
                    break;
            }
            return step;
        }

        private ChannelIntegrationData FetchChannelData(ChannelTypeEnum channelType, string workflowId)
        {
            switch (channelType)
            {
                case ChannelTypeEnum.EffectiveMail:
                    var effmailTrigger = _effmailReadModelService.GetTriggerEffmail(workflowId).GetAwaiter().GetResult().FirstOrDefault();
                    if (effmailTrigger == null) return null!;

                    return new ChannelIntegrationData
                    {
                        ChannelName = channelType.ToString(),
                        IntegrationStatus = effmailTrigger.Status,
                        TemplateId = effmailTrigger.TemplateId,
                        Raw = JsonConvert.SerializeObject(effmailTrigger),
                        Leads = MapLeadsData(effmailTrigger.Leads),
                        File = MapFileInfoData(effmailTrigger.File)
                    };

                case ChannelTypeEnum.EffectiveSms:
                    var effsmsTrigger = _effsmsReadModelService.GetTriggerEffsms(workflowId).GetAwaiter().GetResult().FirstOrDefault();
                    if (effsmsTrigger == null) return null!;

                    return new ChannelIntegrationData
                    {
                        ChannelName = channelType.ToString(),
                        IntegrationStatus = effsmsTrigger.Status,
                        TemplateId = effsmsTrigger.TemplateId,
                        Raw = JsonConvert.SerializeObject(effsmsTrigger),
                        Leads = MapLeadsData(effsmsTrigger.Leads),
                        File = MapFileInfoData(effsmsTrigger.File)
                    };

                case ChannelTypeEnum.EffectivePush:
                    var effpushTrigger = _effpushReadModelService.GetTriggerEffpush(workflowId).GetAwaiter().GetResult().FirstOrDefault();
                    if (effpushTrigger == null) return null!;

                    return new ChannelIntegrationData
                    {
                        ChannelName = channelType.ToString(),
                        IntegrationStatus = effpushTrigger.Status,
                        TemplateId = effpushTrigger.TemplateId,
                        Raw = JsonConvert.SerializeObject(effpushTrigger),
                        Leads = MapLeadsData(effpushTrigger.Leads),
                        File = MapFileInfoData(effpushTrigger.File)
                    };

                case ChannelTypeEnum.EffectiveWhatsApp:
                    var effwhatsappTrigger = _effwhatsappReadModelService.GetTriggerEffwhatsapp(workflowId).GetAwaiter().GetResult().FirstOrDefault();
                    if (effwhatsappTrigger == null) return null!;

                    return new ChannelIntegrationData
                    {
                        ChannelName = channelType.ToString(),
                        IntegrationStatus = effwhatsappTrigger.Status,
                        TemplateId = effwhatsappTrigger.TemplateId,
                        Raw = JsonConvert.SerializeObject(effwhatsappTrigger),
                        Leads = MapLeadsData(effwhatsappTrigger.Leads),
                        File = MapFileInfoData(effwhatsappTrigger.Archive)
                    };

                default:
                    Console.WriteLine($"AVISO: Mapeamento para o canal '{channelType}' não implementado.");
                    return null!;
            }
        }


        private LeadsData MapLeadsData(Domain.Models.Read.Effmail.Leads leads)
        {
            if (leads == null) return null!;
            return new LeadsData
            {
                Success = leads.Success,
                Error = leads.Error,
                Blocked = leads.Blocked,
                Optout = leads.Optout,
                Deduplication = leads.Deduplication
            };
        }

        private LeadsData MapLeadsData(Domain.Models.Read.Effsms.Leads leads)
        {
            if (leads == null) return null!;
            return new LeadsData
            {
                Success = leads.Success,
                Error = leads.Error,
                Blocked = leads.Blocked,
                Optout = leads.Optout,
                Deduplication = leads.Deduplication
            };
        }

        private LeadsData MapLeadsData(Domain.Models.Read.Effpush.Leads leads)
        {
            if (leads == null) return null!;
            return new LeadsData
            {
                Success = leads.Success,
                Error = leads.Error,
                Blocked = leads.Blocked,
                Optout = leads.Optout,
                Deduplication = leads.Deduplication
            };
        }

        private LeadsData MapLeadsData(Domain.Models.Read.Effwhatsapp.Leads leads)
        {
            if (leads == null) return null!;
            return new LeadsData
            {
                Success = leads.Success,
                Error = leads.Error,
                Blocked = leads.Blocked,
                Optout = leads.Optout,
                Deduplication = leads.Deduplication
            };
        }

        private FileInfoData MapFileInfoData(Domain.Models.Read.Effmail.FileInfo fileInfo)
        {
            if (fileInfo == null) return null!;
            return new FileInfoData
            {
                Name = fileInfo.Name,
                StartedAt = fileInfo.StartedAt,
                FinishedAt = fileInfo.FinishedAt,
                Total = fileInfo.Total
            };
        }

        private FileInfoData MapFileInfoData(Domain.Models.Read.Effsms.FileInfo fileInfo)
        {
            if (fileInfo == null) return null!;
            return new FileInfoData
            {
                Name = fileInfo.Name,
                StartedAt = fileInfo.StartedAt,
                FinishedAt = fileInfo.FinishedAt,
                Total = fileInfo.Total
            };
        }

        private FileInfoData MapFileInfoData(Domain.Models.Read.Effpush.FileInfo fileInfo)
        {
            if (fileInfo == null) return null!;
            return new FileInfoData
            {
                Name = fileInfo.Name,
                StartedAt = fileInfo.StartedAt,
                FinishedAt = fileInfo.FinishedAt,
                Total = fileInfo.Total
            };
        }

        private FileInfoData MapFileInfoData(Domain.Models.Read.Effwhatsapp.ArchiveInfo archiveInfo)
        {
            if (archiveInfo == null) return null!;
            return new FileInfoData
            {
                Name = archiveInfo.Name,
                StartedAt = archiveInfo.StartedAt,
                FinishedAt = archiveInfo.FinishedAt,
                Total = archiveInfo.Total
            };
        }
    }
}