namespace CampaignWatchWorker.Domain.Models.Enums
{
    public enum HealthStatusEnum
    {
        Healthy = 0,           // Tudo funcionando normalmente
        Warning = 1,           // Alerta que requer atenção
        Error = 2,             // Erro que impede funcionamento
        Critical = 3           // Erro crítico que requer ação imediata
    }
}
