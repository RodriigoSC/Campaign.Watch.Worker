using System.ComponentModel;


namespace CampaignWatchWorker.Domain.Models.Enums
{
    public enum WorkflowStepTypeEnum
    {
        [Description("Filtro de Dados")]
        Filter,

        [Description("Canal de Comunicação")]
        Channel,

        [Description("Fim da Jornada")]
        End,
    }
}
