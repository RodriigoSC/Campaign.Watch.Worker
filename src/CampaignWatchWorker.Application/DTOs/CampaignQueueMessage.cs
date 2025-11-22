using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CampaignWatchWorker.Application.DTOs
{
    public class CampaignQueueMessage
    {
        // Identificador do Cliente no Mongo (ou o nome, se preferir usar como chave)
        public string ClientId { get; set; }

        // Identificador da Campanha
        public string CampaignId { get; set; }
    }
}
