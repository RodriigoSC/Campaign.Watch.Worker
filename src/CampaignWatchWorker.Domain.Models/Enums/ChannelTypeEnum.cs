using System.ComponentModel;

namespace CampaignWatchWorker.Domain.Models.Enums
{
    public enum ChannelTypeEnum
    {
        [Description("EffectiveMail")]
        EffectiveMail = 1,

        [Description("EffectiveSms")]
        EffectiveSms = 2,

        [Description("EffectivePush")]
        EffectivePush = 3,

        [Description("EffectiveWhatsApp")]
        EffectiveWhatsApp = 5,

        [Description("EffectiveApi")]
        EffectiveApi = 6
    }
}
