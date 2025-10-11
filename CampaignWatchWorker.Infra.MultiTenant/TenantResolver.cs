using System.Net;
using CampaignWatchWorker.Domain.Models.Interfaces;

namespace CampaignWatchWorker.Infra.MultiTenant
{
    public class TenantResolver
    {
        public static ITenant SetupTenant((Tenant content, HttpStatusCode statusCode, Exception ex)? result)
        {
            try
            {
                if (result is null)
                    throw new ArgumentNullException(nameof(result));

                if (result.Value.statusCode == HttpStatusCode.OK)
                    return result.Value.content;
                else
                    throw result.Value.ex;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro no setup do Tenant");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"Type: {ex.GetType().FullName}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                Console.WriteLine(Environment.NewLine);

                throw;
            }
        }
    }
}
