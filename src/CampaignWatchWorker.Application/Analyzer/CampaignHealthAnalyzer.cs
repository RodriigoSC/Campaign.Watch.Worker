using CampaignWatchWorker.Application.Validators;
using CampaignWatchWorker.Domain.Models;
using CampaignWatchWorker.Domain.Models.Diagnostics;
using CampaignWatchWorker.Domain.Models.Enums;
using Microsoft.Extensions.Logging;

namespace CampaignWatchWorker.Application.Analyzer
{
    public class CampaignHealthAnalyzer : ICampaignHealthAnalyzer
    {
        private readonly Dictionary<WorkflowStepTypeEnum, IStepValidator> _validators;
        private readonly ILogger<CampaignHealthAnalyzer> _logger;

        public CampaignHealthAnalyzer(IEnumerable<IStepValidator> validators, ILogger<CampaignHealthAnalyzer> logger)
        {
            _logger = logger;
            _validators = validators.ToDictionary(v => v.SupportedStepType, v => v);
        }

        public async Task<ExecutionDiagnostic> AnalyzeExecutionAsync(ExecutionModel execution, CampaignModel campaign)
        {
            var diagnostic = new ExecutionDiagnostic
            {
                ExecutionId = execution.OriginalExecutionId,
                AnalyzedAt = DateTime.UtcNow,
                StepDiagnostics = new List<StepDiagnostic>()
            };

            try
            {
                foreach (var step in execution.Steps)
                {
                    StepDiagnostic stepDiag;

                    if (Enum.TryParse<WorkflowStepTypeEnum>(step.Type, true, out var stepType))
                    {
                        if (_validators.TryGetValue(stepType, out var validator))
                        {
                            stepDiag = await validator.ValidateAsync(step, execution, campaign);
                        }
                        else
                        {
                            stepDiag = CreateGenericStepDiagnostic(step);
                            _logger.LogWarning($"Nenhum validador encontrado para o tipo de step: {stepType}");
                        }
                    }
                    else
                    {
                        stepDiag = new StepDiagnostic
                        {
                            StepId = step.OriginalStepId,
                            StepName = step.Name,
                            Severity = HealthStatusEnum.Warning,
                            Message = $"Tipo de step desconhecido: {step.Type}",
                            DetectedAt = DateTime.UtcNow
                        };
                        stepDiag.AdditionalData["StepType"] = step.Type;
                    }

                    diagnostic.StepDiagnostics.Add(stepDiag);
                }

                diagnostic.OverallHealth = DetermineOverallHealth(diagnostic.StepDiagnostics);

                diagnostic.Summary = GenerateExecutionSummary(diagnostic, execution);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao analisar execução {execution.OriginalExecutionId}");
                diagnostic.OverallHealth = HealthStatusEnum.Error;
                diagnostic.Summary = $"Erro durante análise: {ex.Message}";
            }

            return diagnostic;
        }

        public async Task<MonitoringHealthStatus> AnalyzeCampaignHealthAsync(CampaignModel campaign, List<ExecutionModel> executions)
        {
            var healthStatus = new MonitoringHealthStatus
            {
                IsFullyVerified = true,
                HasPendingExecution = false,
                HasIntegrationErrors = false,
                LastExecutionWithIssueId = null,
                LastMessage = "Campanha operando normalmente"
            };

            try
            {
                if (executions == null || !executions.Any())
                {
                    if (campaign.CampaignType == CampaignTypeEnum.Pontual)
                    {
                        if (campaign.StatusCampaign == CampaignStatusEnum.Scheduled)
                        {
                            healthStatus.LastMessage = "Campanha pontual agendada, aguardando primeira execução";
                        }
                        else if (campaign.StatusCampaign == CampaignStatusEnum.Draft)
                        {
                            healthStatus.LastMessage = "Campanha em rascunho";
                        }
                        else
                        {
                            healthStatus.HasIntegrationErrors = true;
                            healthStatus.LastMessage = "Campanha sem execuções registradas";
                        }
                    }
                    else
                    {
                        healthStatus.LastMessage = "Campanha recorrente aguardando execução";
                    }

                    return healthStatus;
                }

                var pendingExecutions = executions.Where(e =>
                    e.Status != "Completed" &&
                    e.Status != "Error" &&
                    e.Status != "Canceled").ToList();

                if (pendingExecutions.Any())
                {
                    healthStatus.HasPendingExecution = true;
                }

                var executionsWithErrors = executions.Where(e => e.HasMonitoringErrors).ToList();
                if (executionsWithErrors.Any())
                {
                    healthStatus.HasIntegrationErrors = true;
                    healthStatus.LastExecutionWithIssueId = executionsWithErrors.Last().OriginalExecutionId;
                }

                if (campaign.CampaignType == CampaignTypeEnum.Pontual)
                {
                    healthStatus.LastMessage = AnalyzePonctualCampaign(campaign, executions, healthStatus);
                }
                else
                {
                    healthStatus.LastMessage = AnalyzeRecurrentCampaign(campaign, executions, healthStatus);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao analisar saúde da campanha {campaign.IdCampaign}");
                healthStatus.HasIntegrationErrors = true;
                healthStatus.LastMessage = $"Erro na análise de saúde: {ex.Message}";
            }

            return healthStatus;
        }

        private StepDiagnostic CreateGenericStepDiagnostic(WorkflowStep step)
        {
            var diagnostic = new StepDiagnostic
            {
                StepId = step.OriginalStepId,
                StepName = step.Name,
                DetectedAt = DateTime.UtcNow
            };

            if (!string.IsNullOrEmpty(step.Error))
            {
                diagnostic.Severity = HealthStatusEnum.Error;
                diagnostic.Message = $"Step com erro: {step.Error}";
                diagnostic.AdditionalData["Error"] = step.Error;
            }
            else if (step.Status == "Completed")
            {
                diagnostic.Severity = HealthStatusEnum.Healthy;
                diagnostic.Message = "Step completado";
            }
            else
            {
                diagnostic.Severity = HealthStatusEnum.Healthy;
                diagnostic.Message = $"Step em execução (Status: {step.Status})";
            }

            return diagnostic;
        }

        private HealthStatusEnum DetermineOverallHealth(List<StepDiagnostic> stepDiagnostics)
        {
            if (!stepDiagnostics.Any())
                return HealthStatusEnum.Healthy;

            if (stepDiagnostics.Any(s => s.Severity == HealthStatusEnum.Critical))
                return HealthStatusEnum.Critical;

            if (stepDiagnostics.Any(s => s.Severity == HealthStatusEnum.Error))
                return HealthStatusEnum.Error;

            if (stepDiagnostics.Any(s => s.Severity == HealthStatusEnum.Warning))
                return HealthStatusEnum.Warning;

            return HealthStatusEnum.Healthy;
        }

        private string GenerateExecutionSummary(ExecutionDiagnostic diagnostic, ExecutionModel execution)
        {
            var criticalIssues = diagnostic.StepDiagnostics.Count(s => s.Severity == HealthStatusEnum.Critical);
            var errors = diagnostic.StepDiagnostics.Count(s => s.Severity == HealthStatusEnum.Error);
            var warnings = diagnostic.StepDiagnostics.Count(s => s.Severity == HealthStatusEnum.Warning);

            if (criticalIssues > 0)
            {
                return $"Execução com {criticalIssues} problema(s) crítico(s). Status: {execution.Status}";
            }

            if (errors > 0)
            {
                return $"Execução com {errors} erro(s). Status: {execution.Status}";
            }

            if (warnings > 0)
            {
                return $"Execução com {warnings} alerta(s). Status: {execution.Status}";
            }

            return $"Execução saudável. Status: {execution.Status}";
        }

        private string AnalyzePonctualCampaign(CampaignModel campaign, List<ExecutionModel> executions, MonitoringHealthStatus healthStatus)
        {
            var lastExecution = executions.OrderByDescending(e => e.StartDate).FirstOrDefault();

            if (lastExecution == null)
            {
                return "Campanha pontual sem execuções";
            }

            if (executions.Count > 1)
            {
                return $"AVISO: Campanha pontual com {executions.Count} execuções (esperado: 1)";
            }

            if (campaign.StatusCampaign == CampaignStatusEnum.Completed)
            {
                if (healthStatus.HasIntegrationErrors)
                {
                    return "Campanha completada com problemas de integração detectados";
                }
                return "Campanha pontual executada com sucesso";
            }

            if (campaign.StatusCampaign == CampaignStatusEnum.Executing)
            {
                if (healthStatus.HasIntegrationErrors)
                {
                    return "Campanha em execução com erros detectados";
                }
                return "Campanha pontual em execução";
            }

            if (campaign.StatusCampaign == CampaignStatusEnum.Error)
            {
                return "Campanha pontual com erro na execução";
            }

            if (campaign.StatusCampaign == CampaignStatusEnum.Scheduled)
            {
                if (campaign.Scheduler != null && campaign.Scheduler.StartDateTime > DateTime.UtcNow)
                {
                    return $"Campanha agendada para {campaign.Scheduler.StartDateTime:dd/MM/yyyy HH:mm}";
                }
                return "Campanha agendada";
            }

            return $"Campanha pontual - Status: {campaign.StatusCampaign}";
        }

        private string AnalyzeRecurrentCampaign(CampaignModel campaign, List<ExecutionModel> executions, MonitoringHealthStatus healthStatus)
        {
            var completedExecutions = executions.Count(e => e.Status == "Completed");
            var failedExecutions = executions.Count(e => e.Status == "Error");
            var inProgressExecutions = executions.Count(e =>
                e.Status != "Completed" && e.Status != "Error" && e.Status != "Canceled");

            var lastExecution = executions.OrderByDescending(e => e.StartDate).FirstOrDefault();

            if (inProgressExecutions > 0)
            {
                if (healthStatus.HasIntegrationErrors)
                {
                    return $"Campanha recorrente com execução em andamento apresentando erros. Total de execuções: {executions.Count}";
                }
                return $"Campanha recorrente com execução em andamento. Total de execuções: {executions.Count}";
            }

            if (healthStatus.HasIntegrationErrors)
            {
                return $"Campanha recorrente com problemas detectados. Execuções: {completedExecutions} concluídas, {failedExecutions} com erro";
            }

            if (campaign.Scheduler != null)
            {
                if (DateTime.UtcNow < campaign.Scheduler.StartDateTime)
                {
                    return $"Campanha recorrente aguardando início em {campaign.Scheduler.StartDateTime:dd/MM/yyyy HH:mm}";
                }

                if (campaign.Scheduler.EndDateTime.HasValue && DateTime.UtcNow > campaign.Scheduler.EndDateTime.Value)
                {
                    return $"Campanha recorrente finalizada. Total de {executions.Count} execuções";
                }
            }

            return $"Campanha recorrente ativa. Execuções: {completedExecutions} concluídas, {failedExecutions} com erro";
        }
    }
}
