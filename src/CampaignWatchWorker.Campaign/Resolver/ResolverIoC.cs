using CampaignWatchWorker.Domain.Models.Interfaces.Services.Read.Campaign;
using CampaignWatchWorker.Infra.Campaign.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CampaignWatchWorker.Infra.Campaign.Resolver
{
    public static  class ResolverIoC
    {
        /// <summary>
        /// Adiciona os serviços de leitura de campanha ao contêiner de injeção de dependência.
        /// </summary>
        /// <param name="services">A coleção de serviços para adicionar os registros.</param>
        /// <returns>A mesma coleção de serviços com os novos registros adicionados.</returns>
        public static IServiceCollection AddCampaignRepository(this IServiceCollection services)
        {
            // Registra a implementação do serviço de leitura de dados de campanha.
            services.AddTransient<ICampaignReadModelService, CampaignReadModelService>();

            return services;
        }
    }
}
