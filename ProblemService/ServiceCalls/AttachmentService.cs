using Newtonsoft.Json;
using ProblemService.Models.DTOs;

namespace ProblemService.ServiceCalls
{
    public class AttachmentService : IAttachmentService
    {
        private readonly IConfiguration _configuration;

        public AttachmentService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IEnumerable<AttachmentVO> GetAttachmentsByProblemId(Guid problemId)
        {
            using (HttpClient client = new HttpClient())
            {
                Uri url = new Uri($"{_configuration["Services:AttachmentService"]}api/attachment/problem/{problemId}");
                var response = client.GetAsync(url).Result;
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }
                var content = response.Content.ReadAsStringAsync().Result;
                return JsonConvert.DeserializeObject<IEnumerable<AttachmentVO>>(content);
            }
        }
    }
}
