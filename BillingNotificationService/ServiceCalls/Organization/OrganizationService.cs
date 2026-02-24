using BillingNotificationService.Models.DTOs.Organization;
using Newtonsoft.Json;


namespace BillingNotificationService.ServiceCalls.Organization
{
    public class OrganizationService : IOrganizationService
    {
        private readonly IConfiguration _configuration;

        public OrganizationService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public OrganizationDTO getOrganizationById(Guid id)
        {
            using (HttpClient client = new HttpClient())
            {
                Uri url = new Uri($"{_configuration["Services:OrganizationService"]}api/organization/{id}");
                var response = client.GetAsync(url).Result;
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }
                var content = response.Content.ReadAsStringAsync().Result;
                return JsonConvert.DeserializeObject<OrganizationDTO>(content);
            }
        }
    }
}
