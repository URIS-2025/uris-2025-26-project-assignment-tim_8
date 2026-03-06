using System.Net.Http.Headers;
using System.Text.Json;

namespace SuggestionBoxService.Clients
{
    public class LoggerServiceClient
    {
        private readonly HttpClient _http;

        public LoggerServiceClient(IHttpClientFactory factory)
        {
            _http = factory.CreateClient("LoggerService");
        }

        public async Task TryLogAsync(LogCreationDTO dto, string? bearerHeader, CancellationToken requestCt)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, "/api/logger");

                if (!string.IsNullOrWhiteSpace(bearerHeader))
                    req.Headers.Authorization = AuthenticationHeaderValue.Parse(bearerHeader);

                var opts = new JsonSerializerOptions(JsonSerializerDefaults.Web);
                req.Content = JsonContent.Create(dto, options: opts);

                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(800));
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(requestCt, timeoutCts.Token);

                using var res = await _http.SendAsync(req, linked.Token);
            }
            catch { }
        }
    }
}
}
