using Newtonsoft.Json;
using ProblemService.Models.DTOs;

namespace ProblemService.ServiceCalls
{
    public class ProblemCommentAuthorUserService : IProblemCommentAuthorUserService
    {
        private readonly IConfiguration _configuration;

        public ProblemCommentAuthorUserService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public ProblemCommentAuthorUserVO GetUserById(Guid id)
        {
            using (HttpClient client = new HttpClient())
            {
                Uri url = new Uri($"{_configuration["Services:UserService"]}api/user/{id}");
                var response = client.GetAsync(url).Result;
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }
                var content = response.Content.ReadAsStringAsync().Result;
                return JsonConvert.DeserializeObject<ProblemCommentAuthorUserVO>(content);
            }
        }
    }
}
