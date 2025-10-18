using CampaignWatchWorker.Domain.Models;
using CampaignWatchWorker.Domain.Models.Diagnostics;
using CampaignWatchWorker.Domain.Models.Enums;

namespace CampaignWatchWorker.Application.Validators
{
    public interface IStepValidator
    {
        /// <summary>
        /// Tipo de step que este validador suporta.
        /// </summary>
        WorkflowStepTypeEnum SupportedStepType { get; }

        /// <summary>
        /// Valida um step específico dentro do contexto de uma execução e campanha.
        /// </summary>
        /// <param name="step">O step a ser validado</param>
        /// <param name="execution">A execução que contém o step</param>
        /// <param name="campaign">A campanha associada</param>
        /// <returns>Um diagnóstico detalhado do step</returns>
        Task<StepDiagnostic> ValidateAsync(WorkflowStep step,ExecutionModel execution,CampaignModel campaign);
    }
}
