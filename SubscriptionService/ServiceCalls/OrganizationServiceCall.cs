using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using System.Net.Http;

namespace SubscriptionService.ServiceCalls
{
    using global::SubscriptionService.Models.ExternalDTOs;
    using System.Text.Json;

    namespace SubscriptionService.ServiceCalls
    {
        public class OrganizationServiceCall
        {
            private readonly HttpClient _httpClient;

            public OrganizationServiceCall(IHttpClientFactory httpClientFactory)
            {
                _httpClient = httpClientFactory.CreateClient("OrganizationService");
            }

            public async Task<Guid> GetOrganizationId(Guid organizationId)
            {
                var response = await _httpClient.GetAsync($"/api/organization/{organizationId}");

                if (!response.IsSuccessStatusCode)
                    throw new Exception("Organization not found");

                var content = await response.Content.ReadAsStringAsync();

                var result = JsonSerializer.Deserialize<OrganizationDTO>(content, // dodati DTO za Organizaciju unutar SubServisa
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result == null)
                    throw new Exception("Invalid response from Organization service");

                return result.Id;
            }
        }
    }
}