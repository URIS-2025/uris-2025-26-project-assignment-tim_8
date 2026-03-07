using System.Net.Http;
using System.Net.Http.Json;
using SubscriptionService.Models.ExternalDTOs;

namespace SubscriptionService.ServiceCalls
{
    public class BillingServiceCall
    {
        private readonly HttpClient _httpClient;

        public BillingServiceCall(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("BillingNotificationService");
        }

        public async Task CreateBillingNotificationAsync(BillingNotificationCreateDTO dto)
        {
            var response = await _httpClient.PostAsJsonAsync(
                "/api/billingnotification", dto);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();

                throw new Exception(
                    $"BillingService error: {response.StatusCode} - {errorContent}");
            }
        }
    }
}