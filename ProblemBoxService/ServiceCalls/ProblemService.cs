using Newtonsoft.Json;
using ProblemBoxService.Models;
using ProblemBoxService.Models.DTOs;

namespace ProblemBoxService.ServiceCalls
{
    public class ProblemService : IProblemService
    {
        private readonly IConfiguration _configuration;

        public ProblemService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IEnumerable<ProblemVO> GetProblemsByProblemBoxId(Guid problemBoxId)
        {
            using (HttpClient client = new HttpClient())
            {
                Uri url = new Uri($"{_configuration["Services:ProblemService"]}api/problem/problembox/{problemBoxId}");
                var response = client.GetAsync(url).Result;
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }
                var content = response.Content.ReadAsStringAsync().Result;
                return JsonConvert.DeserializeObject<IEnumerable<ProblemVO>>(content);
            }
        }
    }
}