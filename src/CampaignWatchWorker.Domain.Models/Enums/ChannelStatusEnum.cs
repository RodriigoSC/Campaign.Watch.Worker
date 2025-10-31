using System.ComponentModel;


namespace CampaignWatchWorker.Domain.Models.Enums
{
    public enum ChannelStatusEnum
    {
        [Description("Rascunho")]
        Draft,

        [Description("Agendado")]
        Scheduler,

        [Description("Em Progresso")]
        InProgress,

        [Description("Concluído")]
        Concluded,

        [Description("Erro")]
        Error
    }
}
