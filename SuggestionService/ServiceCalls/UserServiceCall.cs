using System.Text.Json;
using SuggestionService.Models.DTOs;

namespace SuggestionService.ServiceCalls
{
    public class UserServiceCall : IUserServiceCall
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public UserServiceCall(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<UserDTO> GetUserById(Guid userId)
        {
            var baseUrl = _configuration["ServiceUrls:OrganizationService"];
            var response = await _httpClient.GetAsync($"{baseUrl}api/User/{userId}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<UserDTO>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            return null;
        }
    }
}