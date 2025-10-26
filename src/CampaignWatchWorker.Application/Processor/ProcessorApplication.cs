// CampaignWatchWorker.Application/Processor/ProcessorApplication.cs
using CampaignWatchWorker.Application.Analyzer;
using CampaignWatchWorker.Application.Mappers;
using CampaignWatchWorker.Domain.Models;
using CampaignWatchWorker.Domain.Models.Enums;
using CampaignWatchWorker.Domain.Models.Interfaces.Repositories;
using CampaignWatchWorker.Domain.Models.Interfaces.Services.Read.Campaign;
using MongoDB.Bson;
//using Microsoft.Extensions.Logging;

namespace CampaignWatchWorker.Application.Processor
{
    public class ProcessorApplication : IProcessorApplication
    {
        private readonly ICampaignReadModelService _campaignReadModelService;
        private readonly ICampaignModelRepository _campaignModelRepository;
        private readonly IExecutionModelRepository _executionModelRepository;
        private readonly ICampaignMapper _campaignMapper;
        private readonly ICampaignHealthAnalyzer _healthAnalyzer;
        //private readonly ILogger<ProcessorApplication> _logger;

        public ProcessorApplication(
            ICampaignReadModelService campaignReadModelService,
            ICampaignModelRepository campaignModelRepository,
            IExecutionModelRepository executionModelRepository,
            ICampaignMapper campaignMapper,
            ICampaignHealthAnalyzer healthAnalyzer/*,
            ILogger<ProcessorApplication> logger*/)
        {
            _campaignReadModelService = campaignReadModelService;
            _campaignModelRepository = campaignModelRepository;
            _executionModelRepository = executionModelRepository;
            _campaignMapper = campaignMapper;
            _healthAnalyzer = healthAnalyzer;
            //_logger = logger;
        }

        public void Process(object obj)
        {
            string campaignSourceId = null!; // Renomeado para clareza, inicializado como não nulo
            CampaignModel? campaignModel = null; // Modelo de domínio principal

            try
            {
                // Tenta obter o ID da campanha do objeto recebido
                campaignSourceId = obj?.ToString()?.Trim('"')!; // Remove aspas extras se vier de JSON string
                if (string.IsNullOrEmpty(campaignSourceId))
                {
                    Console.WriteLine("ID da Campanha nulo ou vazio. Mensagem ignorada.");
                    //_logger.LogWarning("ID da Campanha nulo ou vazio. Mensagem ignorada.");
                    return;
                }

                Console.WriteLine($"Iniciando processamento para a Campanha Source ID: {campaignSourceId}");
                //_logger.LogInformation($"Iniciando processamento para a Campanha Source ID: {campaignSourceId}");

                // --- PASSO 1: Buscar dados da campanha na ORIGEM ---
                var campaignReadModel = _campaignReadModelService.GetCampaignById(campaignSourceId).GetAwaiter().GetResult();
                if (campaignReadModel == null)
                {
                    Console.WriteLine($"Campanha com Source ID {campaignSourceId} não encontrada no sistema de origem.");
                    //_logger.LogWarning($"Campanha com Source ID {campaignSourceId} não encontrada no sistema de origem.");
                    return; // Não há o que processar
                }

                // --- PASSO 2: Mapear para o modelo de domínio ---
                campaignModel = _campaignMapper.MapToCampaignModel(campaignReadModel);
                if (campaignModel == null)
                {
                    Console.WriteLine($"Erro no mapeamento da Campanha Source ID: {campaignSourceId}");
                    //_logger.LogError($"Erro no mapeamento da Campanha Source ID: {campaignSourceId}");
                    return; // Não pode continuar sem o modelo mapeado
                }

                // --- PASSO 3: Garantir que a campanha exista no BD de Monitoramento e obter seu ObjectId ---
                var existingCampaign = _campaignModelRepository.ObterCampanhaPorIdAsync(campaignModel.ClientName, campaignModel.IdCampaign).GetAwaiter().GetResult();
                bool isNewCampaign = existingCampaign == null;
                ObjectId campaignMonitoringId;

                if (isNewCampaign)
                {
                    Console.WriteLine($"Campanha '{campaignModel.Name}' ({campaignModel.IdCampaign}) não encontrada no monitoramento. Criando...");
                    //_logger.LogInformation($"Campanha '{campaignModel.Name}' ({campaignModel.IdCampaign}) não encontrada no monitoramento. Criando...");
                    // Define datas de criação apenas se for nova
                    campaignModel.CreatedAt = DateTime.UtcNow;
                    campaignModel.FirstMonitoringAt = DateTime.UtcNow;
                    campaignModel.ModifiedAt = DateTime.UtcNow; // Definir ModifiedAt na criação também
                    // Tenta criar (usando o método Atualizar com upsert=true é mais seguro para concorrência)
                    var success = _campaignModelRepository.AtualizarCampanhaAsync(campaignModel).GetAwaiter().GetResult();
                    if (!success || campaignModel.Id == ObjectId.Empty)
                    {
                        Console.WriteLine($"Falha ao criar/obter ID da nova campanha '{campaignModel.Name}' ({campaignModel.IdCampaign}).");
                        //_logger.LogError($"Falha ao criar/obter ID da nova campanha '{campaignModel.Name}' ({campaignModel.IdCampaign}).");
                        return; // Não pode continuar sem o ID
                    }
                    campaignMonitoringId = campaignModel.Id; // ID foi definido pelo AtualizarCampanhaAsync (upsert)
                    Console.WriteLine($"Nova campanha criada com Monitoring ID: {campaignMonitoringId}");
                    //_logger.LogInformation($"Nova campanha criada com Monitoring ID: {campaignMonitoringId}");
                }
                else
                {
                    campaignMonitoringId = existingCampaign!.Id; // Usa o ID existente
                    // Atualiza os campos que podem mudar, mas mantém CreatedAt e FirstMonitoringAt
                    campaignModel.Id = campaignMonitoringId; // Garante que o ID correto está no modelo
                    campaignModel.CreatedAt = existingCampaign.CreatedAt; // Mantém a data de criação original
                    campaignModel.FirstMonitoringAt = existingCampaign.FirstMonitoringAt ?? DateTime.UtcNow; // Mantém ou define se era nulo
                    Console.WriteLine($"Campanha '{campaignModel.Name}' ({campaignModel.IdCampaign}) encontrada com Monitoring ID: {campaignMonitoringId}");
                    //_logger.LogInformation($"Campanha '{campaignModel.Name}' ({campaignModel.IdCampaign}) encontrada com Monitoring ID: {campaignMonitoringId}");
                }

                // --- PASSO 4: Buscar e processar execuções da ORIGEM ---
                var executionsRead = _campaignReadModelService.GetExecutionsByCampaign(campaignSourceId).GetAwaiter().GetResult();
                var executionModels = new List<ExecutionModel>();
                int processedExecutionCount = 0;
                int errorExecutionCount = 0;


                if (executionsRead != null && executionsRead.Any())
                {
                    Console.WriteLine($"Encontradas {executionsRead.Count()} execuções para a campanha {campaignSourceId}.");
                    //_logger.LogInformation($"Encontradas {executionsRead.Count()} execuções para a campanha {campaignSourceId}.");

                    foreach (var executionRead in executionsRead)
                    {
                        ExecutionModel? executionModel = null; // Modelo da execução atual
                        try
                        {
                            // Mapear execução, AGORA passando o ID de monitoramento CORRETO
                            executionModel = _campaignMapper.MapToExecutionModel(executionRead, campaignMonitoringId);
                            if (executionModel == null)
                            {
                                Console.WriteLine($"Erro no mapeamento da Execução Source ID: {executionRead.ExecutionId} para Campanha Source ID: {campaignSourceId}");
                                //_logger.LogWarning($"Erro no mapeamento da Execução Source ID: {executionRead.ExecutionId} para Campanha Source ID: {campaignSourceId}");
                                continue; // Pula para a próxima execução
                            }

                            // Analisar saúde da execução
                            var diagnostic = _healthAnalyzer.AnalyzeExecutionAsync(executionModel, campaignModel).GetAwaiter().GetResult();

                            // Define se há erros com base no resultado da análise
                            executionModel.HasMonitoringErrors = diagnostic.OverallHealth == HealthStatusEnum.Error ||
                                                                 diagnostic.OverallHealth == HealthStatusEnum.Critical;

                            // Log do diagnóstico (se habilitado)
                            //LogExecutionDiagnostic(diagnostic, executionModel);

                            // Persistir/Atualizar execução no BD de Monitoramento
                            var execSuccess = _executionModelRepository.AtualizarExecucaoAsync(executionModel).GetAwaiter().GetResult();
                            if (execSuccess)
                            {
                                executionModels.Add(executionModel); // Adiciona à lista apenas se salva com sucesso
                                processedExecutionCount++;
                                if (executionModel.HasMonitoringErrors) errorExecutionCount++;
                                Console.WriteLine($"Execução {executionModel.OriginalExecutionId} processada. Status Saúde: {diagnostic.OverallHealth}");
                                //_logger.LogInformation($"Execução {executionModel.OriginalExecutionId} processada. Status Saúde: {diagnostic.OverallHealth}");
                            }
                            else
                            {
                                Console.WriteLine($"Falha ao salvar/atualizar Execução {executionModel.OriginalExecutionId}.");
                                //_logger.LogError($"Falha ao salvar/atualizar Execução {executionModel.OriginalExecutionId}.");
                            }

                        }
                        catch (Exception ex)
                        {
                            string execId = executionRead?.ExecutionId.ToString() ?? "desconhecido";
                            Console.WriteLine($"Erro ao processar a Execução Source ID: {execId} - {ex.Message}");
                            //_logger.LogError(ex, $"Erro ao processar a Execução Source ID: {execId}");
                            // Tenta marcar a execução com erro se o modelo foi mapeado
                            if (executionModel != null)
                            {
                                executionModel.HasMonitoringErrors = true;
                                executionModel.Status = "Error"; // Ou algum status indicando falha no processamento
                                _executionModelRepository.AtualizarExecucaoAsync(executionModel).GetAwaiter().GetResult(); // Tenta salvar com erro
                                errorExecutionCount++; // Conta como erro
                            }
                            continue; // Pula para a próxima execução
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Nenhuma execução encontrada para a campanha {campaignSourceId}.");
                    //_logger.LogInformation($"Nenhuma execução encontrada para a campanha {campaignSourceId}.");
                }

                // --- PASSO 5: Analisar saúde GERAL da campanha com base nas execuções processadas ---
                // Precisamos recarregar a campanha para pegar o estado mais atualizado antes da análise final,
                // especialmente se ela foi criada neste ciclo.
                campaignModel = _campaignModelRepository.ObterCampanhaPorIdAsync(campaignModel.ClientName, campaignModel.IdCampaign).GetAwaiter().GetResult();
                if (campaignModel == null)
                {
                    Console.WriteLine($"Erro: Campanha {campaignSourceId} desapareceu do banco de monitoramento após processar execuções.");
                    //_logger.LogError($"Campanha {campaignSourceId} desapareceu do banco de monitoramento após processar execuções.");
                    return;
                }

                var campaignHealth = _healthAnalyzer.AnalyzeCampaignHealthAsync(campaignModel, executionModels).GetAwaiter().GetResult();
                campaignModel.HealthStatus = campaignHealth;
                campaignModel.LastCheckMonitoring = DateTime.UtcNow;
                campaignModel.ModifiedAt = DateTime.UtcNow; // Atualiza ModifiedAt da campanha

                // Atualiza contadores de execução
                campaignModel.TotalExecutionsProcessed = processedExecutionCount; // Ou buscar contagem do BD se necessário
                campaignModel.ExecutionsWithErrors = errorExecutionCount; // Ou buscar contagem do BD se necessário


                // Definir próxima checagem baseado na saúde e tipo
                campaignModel.NextExecutionMonitoring = CalculateNextCheck(campaignModel);

                // --- PASSO 6: Atualizar a campanha no BD de Monitoramento com o status de saúde e próxima checagem ---
                var finalUpdateSuccess = _campaignModelRepository.AtualizarCampanhaAsync(campaignModel).GetAwaiter().GetResult();
                if (!finalUpdateSuccess)
                {
                    Console.WriteLine($"Falha ao atualizar o status final da campanha '{campaignModel.Name}' ({campaignModel.IdCampaign}).");
                    //_logger.LogError($"Falha ao atualizar o status final da campanha '{campaignModel.Name}' ({campaignModel.IdCampaign}).");
                }


                // --- PASSO 7: Log final ---
                //LogCampaignSummary(campaignModel, campaignHealth, processedExecutionCount);
                Console.WriteLine($"Campanha '{campaignModel.Name}' processada. Status Saúde: {campaignHealth?.LastMessage}. Próxima verificação: {campaignModel.NextExecutionMonitoring}");
                //_logger.LogInformation($"Campanha '{campaignModel.Name}' processada com sucesso. Próxima verificação: {campaignModel.NextExecutionMonitoring}");
            }
            catch (Exception ex)
            {
                string campaignNameToLog = campaignModel?.Name ?? campaignSourceId ?? "desconhecido";
                Console.WriteLine($"ERRO FATAL ao processar a mensagem para a Campanha: {campaignNameToLog} - {ex.Message} \n {ex.StackTrace}");
                //_logger.LogError(ex, $"ERRO FATAL ao processar a mensagem para a Campanha: {campaignNameToLog}");

                // Opcional: Tentar atualizar a campanha com status de erro fatal
                if (campaignModel != null && campaignModel.Id != ObjectId.Empty)
                {
                    try
                    {
                        campaignModel.HealthStatus ??= new MonitoringHealthStatus();
                        campaignModel.HealthStatus.HasIntegrationErrors = true;
                        campaignModel.HealthStatus.LastMessage = $"Erro fatal no processamento: {ex.Message}";
                        campaignModel.MonitoringStatus = MonitoringStatusEnum.Failed; // Marcar como falha
                        campaignModel.LastCheckMonitoring = DateTime.UtcNow;
                        campaignModel.NextExecutionMonitoring = DateTime.UtcNow.AddMinutes(15); // Tentar novamente em 15 min
                        _campaignModelRepository.AtualizarCampanhaAsync(campaignModel).GetAwaiter().GetResult();
                    }
                    catch (Exception updateEx)
                    {
                        Console.WriteLine($"Falha ao tentar atualizar campanha com erro fatal: {updateEx.Message}");
                        //_logger.LogError(updateEx, "Falha ao tentar atualizar campanha com erro fatal.");
                    }
                }
            }
        }

        /*private void LogExecutionDiagnostic(ExecutionDiagnostic diagnostic, ExecutionModel execution)
        {
            var logLevel = diagnostic.OverallHealth switch
            {
                HealthStatusEnum.Critical => LogLevel.Critical,
                HealthStatusEnum.Error => LogLevel.Error,
                HealthStatusEnum.Warning => LogLevel.Warning,
                _ => LogLevel.Information
            };

            _logger.Log(logLevel, $"Diagnóstico da Execução {execution.OriginalExecutionId}: {diagnostic.Summary}");

            // Detalhar problemas encontrados
            foreach (var stepDiag in diagnostic.StepDiagnostics.Where(d => d.Severity != HealthStatusEnum.Healthy))
            {
                _logger.Log(
                    ConvertHealthToLogLevel(stepDiag.Severity),
                    $"  Step '{stepDiag.StepName}' ({stepDiag.StepId}): {stepDiag.Message}"
                );
            }
        }*/

        /*private void LogCampaignSummary(CampaignModel campaign, MonitoringHealthStatus health, int executionCount)
        {
            var campaignType = campaign.CampaignType == CampaignTypeEnum.Pontual ? "Pontual" : "Recorrente";

            _logger.LogInformation($"=== RESUMO DA CAMPANHA ===");
            _logger.LogInformation($"Nome: {campaign.Name}");
            _logger.LogInformation($"Tipo: {campaignType}");
            _logger.LogInformation($"Status: {campaign.StatusCampaign}");
            _logger.LogInformation($"Execuções processadas: {executionCount}");
            _logger.LogInformation($"Saúde: {health.LastMessage}");

            if (health.HasIntegrationErrors)
            {
                _logger.LogWarning($"⚠️ Campanha com problemas de integração detectados");
            }

            if (health.HasPendingExecution)
            {
                _logger.LogInformation($"⏳ Há execução pendente ou em andamento");
            }

            _logger.LogInformation($"Próxima verificação agendada para: {campaign.NextExecutionMonitoring:dd/MM/yyyy HH:mm}");
            _logger.LogInformation($"========================");
        }*/

        /*private DateTime? CalculateNextCheck(CampaignModel campaign)
        {
            var now = DateTime.UtcNow;

            // Se a campanha está inativa ou deletada, não precisa checar
            if (!campaign.IsActive || campaign.IsDeleted)
            {
                return null;
            }

            // Se há problemas críticos, verificar em 5 minutos
            if (campaign.HealthStatus.HasIntegrationErrors)
            {
                return now.AddMinutes(5);
            }

            // Campanha pontual
            if (campaign.CampaignType == CampaignTypeEnum.Pontual)
            {
                // Se já executou e está completa, não precisa mais checar
                if (campaign.StatusCampaign == CampaignStatusEnum.Completed)
                {
                    return null;
                }

                // Se está agendada para o futuro, verificar 5 minutos antes
                if (campaign.Scheduler != null && campaign.Scheduler.StartDateTime > now)
                {
                    return campaign.Scheduler.StartDateTime.AddMinutes(-5);
                }

                // Se está em execução, verificar a cada 10 minutos
                if (campaign.StatusCampaign == CampaignStatusEnum.Executing)
                {
                    return now.AddMinutes(10);
                }

                // Padrão: verificar em 30 minutos
                return now.AddMinutes(30);
            }

            // Campanha recorrente
            if (campaign.CampaignType == CampaignTypeEnum.Recorrente)
            {
                // Se há execução pendente, verificar mais frequentemente
                if (campaign.HealthStatus.HasPendingExecution)
                {
                    return now.AddMinutes(10);
                }

                // Se está fora do período de recorrência, não verificar
                if (campaign.Scheduler != null)
                {
                    if (now < campaign.Scheduler.StartDateTime)
                    {
                        return campaign.Scheduler.StartDateTime.AddMinutes(-5);
                    }

                    if (campaign.Scheduler.EndDateTime.HasValue && now > campaign.Scheduler.EndDateTime.Value)
                    {
                        return null;
                    }
                }

                // Verificação padrão a cada hora para campanhas recorrentes
                return now.AddHours(1);
            }

            // Padrão genérico
            return now.AddMinutes(30);
        }*/

        private DateTime? CalculateNextCheck(CampaignModel campaign)
        {
            var now = DateTime.UtcNow;

            // Se a campanha está inativa ou deletada, não precisa checar
            if (!campaign.IsActive || campaign.IsDeleted)
            {
                return null;
            }

            // Se há problemas críticos/erros de integração, verificar mais cedo
            if (campaign.HealthStatus?.HasIntegrationErrors == true)
            {
                //_logger.LogWarning($"Campanha {campaign.Name} com erro de integração. Verificando novamente em 5 minutos.");
                return now.AddMinutes(5);
            }

            // Campanha pontual
            if (campaign.CampaignType == CampaignTypeEnum.Pontual)
            {
                // Se já executou e está completa (sem erros), não precisa mais checar
                if (campaign.StatusCampaign == CampaignStatusEnum.Completed && campaign.HealthStatus?.HasIntegrationErrors == false)
                {
                    //_logger.LogInformation($"Campanha pontual {campaign.Name} concluída sem erros. Não agendando nova verificação.");
                    return null;
                }

                // Se está agendada para o futuro, verificar 5 minutos antes
                if (campaign.Scheduler != null && campaign.Scheduler.StartDateTime > now)
                {
                    //_logger.LogInformation($"Campanha pontual {campaign.Name} agendada para {campaign.Scheduler.StartDateTime}. Verificando 5 min antes.");
                    return campaign.Scheduler.StartDateTime.AddMinutes(-5);
                }

                // Se está em execução, verificar a cada 10 minutos
                if (campaign.StatusCampaign == CampaignStatusEnum.Executing || campaign.HealthStatus?.HasPendingExecution == true)
                {
                    //_logger.LogInformation($"Campanha pontual {campaign.Name} em execução ou pendente. Verificando em 10 minutos.");
                    return now.AddMinutes(10);
                }

                // Padrão para pontual (ex: agendada mas já passou do tempo, erro não crítico): verificar em 30 minutos
                //_logger.LogInformation($"Campanha pontual {campaign.Name} - status {campaign.StatusCampaign}. Verificação padrão em 30 minutos.");
                return now.AddMinutes(30);
            }

            // Campanha recorrente
            if (campaign.CampaignType == CampaignTypeEnum.Recorrente)
            {
                // Se há execução pendente (atrasada), verificar mais frequentemente
                if (campaign.HealthStatus?.HasPendingExecution == true)
                {
                    //_logger.LogWarning($"Campanha recorrente {campaign.Name} com execução pendente/atrasada. Verificando em 10 minutos.");
                    return now.AddMinutes(10);
                }

                // Se está fora do período de recorrência, não verificar
                if (campaign.Scheduler != null)
                {
                    if (now < campaign.Scheduler.StartDateTime)
                    {
                        //_logger.LogInformation($"Campanha recorrente {campaign.Name} aguardando início em {campaign.Scheduler.StartDateTime}. Verificando 5 min antes.");
                        return campaign.Scheduler.StartDateTime.AddMinutes(-5); // Verificar pouco antes de começar
                    }

                    if (campaign.Scheduler.EndDateTime.HasValue && now > campaign.Scheduler.EndDateTime.Value)
                    {
                        //_logger.LogInformation($"Campanha recorrente {campaign.Name} finalizada em {campaign.Scheduler.EndDateTime}. Não agendando nova verificação.");
                        return null; // Já terminou o período
                    }
                }

                // Verificação padrão a cada hora para campanhas recorrentes ativas e sem problemas
                //_logger.LogInformation($"Campanha recorrente {campaign.Name} ativa. Verificação padrão em 1 hora.");
                return now.AddHours(1);
            }

            // Padrão genérico (caso algo não se encaixe)
            //_logger.LogWarning($"Campanha {campaign.Name} - tipo desconhecido ou estado não tratado. Verificação padrão em 30 minutos.");
            return now.AddMinutes(30);
        }

        /*private LogLevel ConvertHealthToLogLevel(HealthStatusEnum health)
        {
            return health switch
            {
                HealthStatusEnum.Critical => LogLevel.Critical,
                HealthStatusEnum.Error => LogLevel.Error,
                HealthStatusEnum.Warning => LogLevel.Warning,
                _ => LogLevel.Information
            };
        }*/
    }
}