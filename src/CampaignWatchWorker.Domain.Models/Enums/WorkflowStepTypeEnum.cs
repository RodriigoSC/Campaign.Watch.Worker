using System.ComponentModel;


namespace CampaignWatchWorker.Domain.Models.Enums
{
    public enum WorkflowStepTypeEnum
    {
        [Description("Filtro")]
        Filter = 0,

        [Description("Canal")]
        Channel = 1,

        [Description("Espera")]
        Wait = 2,

        [Description("Decisão por Condição")]
        DecisionSplit = 3,

        [Description("Término")]
        End = 4,

        [Description("Saída multicanal baseado em filtros públicos")]
        AudienceFilterBasedChannelFork = 5,

        [Description("Datado")]
        Dated = 6,

        [Description("Atualização de contato")]
        ContactUpdate = 7,

        [Description("Divisões randômicas")]
        RandomSplit = 9,

        [Description("Seletor inteligente de canal de envio")]
        SmartSendingChannelSelector = 10,

        [Description("Múltiplas rotas paralelas")]
        MultipleParallelRoutes = 11,

        [Description("Entrada por API")]
        InputAPI = 12,

        [Description("Filtro de marcação")]
        TaggingFilter = 13,

        [Description("Join")]
        Join = 14,

        [Description("Contagem dinâmica")]
        DynamicCount = 15,

        [Description("Exportação de Público")]
        PublicExport = 16,

        [Description("Grupo de Controle")]
        ControlGroup = 17,

        [Description("Entregabilidade")]
        Deliverability = 18,

        [Description("Divisões randômicas limitadas")]
        RandomSplitLimited = 19
    }
}
