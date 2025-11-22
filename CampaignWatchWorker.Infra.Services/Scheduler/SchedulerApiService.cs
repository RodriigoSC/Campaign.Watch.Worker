using CampaignWatchWorker.Domain.Models.Configuration;
using CampaignWatchWorker.Domain.Models.Interfaces.Services.Scheduler;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;


namespace CampaignWatchWorker.Infra.Services.Scheduler
{
    public class SchedulerApiService : ISchedulerApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<SchedulerApiService> _logger;

        public SchedulerApiService(HttpClient httpClient, ILogger<SchedulerApiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task ScheduleExecutionAsync(ScheduleRequest request)
        {
            /*var response = await _httpClient.PostAsJsonAsync("api/scheduler/schedule", request);

            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Erro na API Scheduler: {response.StatusCode} - {content}");
            }*/

        }
    }
}
