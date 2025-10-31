namespace CampaignWatchWorker.Domain.Models.Enums
{
    public enum DiagnosticTypeEnum
    {
        StepTimeout = 1,           // Step rodando há muito tempo
        StepFailed = 2,            // Step falhou com erro
        ExecutionDelayed = 3,      // Execução atrasada
        MissingScheduler = 4,      // Scheduler ausente
        IncompleteExecution = 5,   // Execução incompleta
        IntegrationError = 6,      // Erro de integração com canal
        WaitStepMissed = 7,        // Step de espera não executado
        FilterStuck = 8,           // Filtro travado em consulta
        CampaignNotFinalized = 9   // Campanha não finalizada corretamente
    }
}
