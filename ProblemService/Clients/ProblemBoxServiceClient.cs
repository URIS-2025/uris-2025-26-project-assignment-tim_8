using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ProblemService.Models.DTOs;

namespace ProblemService.Clients
{
    public class ProblemBoxServiceClient
    {
        private readonly HttpClient _http;

        public ProblemBoxServiceClient() { }

        public ProblemBoxServiceClient(IHttpClientFactory factory)
        {
            _http = factory.CreateClient("ProblemBoxService");
        }

        // Returns true ONLY if the box is confirmed Active. Fail-closed: any failure -> false.
        public virtual async Task<bool> IsBoxActiveAsync(Guid boxId, string? bearerHeader, CancellationToken requestCt)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/ProblemBox/{boxId}");
                if (!string.IsNullOrWhiteSpace(bearerHeader))
                    req.Headers.Authorization = AuthenticationHeaderValue.Parse(bearerHeader);

                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(800));
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(requestCt, timeoutCts.Token);

                using var res = await _http.SendAsync(req, linked.Token);
                if (!res.IsSuccessStatusCode) return false; // fail-closed (e.g. 404)

                var box = await res.Content.ReadFromJsonAsync<ProblemBoxStatusVO>(
                    new JsonSerializerOptions(JsonSerializerDefaults.Web), linked.Token);
                return box != null && box.Status == 0; // Active only
            }
            catch { return false; } // fail-closed (timeout/unreachable)
        }
    }
}
