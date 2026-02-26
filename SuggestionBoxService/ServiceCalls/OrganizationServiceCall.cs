using System.Net.Http;
using System.Text.Json;
using SuggestionBoxService.Models.ExternalDTOs;

namespace SuggestionBoxService.ServiceCalls
{
    public class OrganizationServiceCall
    {
        private readonly HttpClient _httpClient;

        public OrganizationServiceCall(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("OrganizationService");
        }

        public async Task<OrganizationDTO> GetOrganizationById(Guid id)
        {
            var response = await _httpClient.GetAsync($"/api/organization/{id}");

            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();

            var organization = JsonSerializer.Deserialize<OrganizationDTO>(
                content,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return organization;
        }

        public async Task<bool> OrganizationExists(Guid id)
        {
            var response = await _httpClient.GetAsync($"/api/organization/{id}");

            return response.IsSuccessStatusCode;
        }
    }
}