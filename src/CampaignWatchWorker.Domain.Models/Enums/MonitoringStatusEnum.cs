using System.ComponentModel;

namespace CampaignWatchWorker.Domain.Models.Enums
{
    public enum MonitoringStatusEnum
    {
        [Description("Pendente")]
        Pending,

        [Description("Em andamento")]
        InProgress,

        [Description("Aguardando próxima execução")]
        WaitingForNextExecution,

        [Description("Concluído")]
        Completed,

        [Description("Falha")]
        Failed,

        [Description("Execução atrasada")]
        ExecutionDelayed
    }
}
