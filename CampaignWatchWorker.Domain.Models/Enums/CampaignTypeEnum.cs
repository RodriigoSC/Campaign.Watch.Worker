using System.ComponentModel;

namespace CampaignWatchWorker.Domain.Models.Enums
{
    public enum CampaignTypeEnum
    {        
        [Description("Pontual")]
        Pontual = 0,

        [Description("Recorrente")]
        Recorrente = 1
    }
}
