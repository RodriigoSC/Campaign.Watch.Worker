using System.ComponentModel;


namespace CampaignWatchWorker.Domain.Models.Enums
{
    public enum CampaignStatusEnum
    {

        [Description("Rascunho")]
        Draft = 0,

        [Description("Concluída")]
        Completed = 1,

        [Description("Erro")]
        Error = 3,

        [Description("Em Execução")]
        Executing = 5,

        [Description("Agendada")]
        Scheduled = 7,

        [Description("Cancelada")]
        Canceled = 8,

        [Description("Cancelando")]
        Canceling = 9
    }
}
