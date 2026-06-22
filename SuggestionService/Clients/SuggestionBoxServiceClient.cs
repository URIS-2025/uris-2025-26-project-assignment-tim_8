using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using SuggestionService.Models.DTOs;

namespace SuggestionService.Clients
{
    public class SuggestionBoxServiceClient
    {
        private readonly HttpClient _http;

        public SuggestionBoxServiceClient() { }

        public SuggestionBoxServiceClient(IHttpClientFactory factory)
        {
            _http = factory.CreateClient("SuggestionBoxService");
        }

        // Returns true ONLY if the box is confirmed Active. Fail-closed: any failure -> false.
        public virtual async Task<bool> IsBoxActiveAsync(Guid boxId, string? bearerHeader, CancellationToken requestCt)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/SuggestionBox/{boxId}");
                if (!string.IsNullOrWhiteSpace(bearerHeader))
                    req.Headers.Authorization = AuthenticationHeaderValue.Parse(bearerHeader);

                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(800));
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(requestCt, timeoutCts.Token);

                using var res = await _http.SendAsync(req, linked.Token);
                if (!res.IsSuccessStatusCode) return false; // fail-closed (e.g. 404)

                var box = await res.Content.ReadFromJsonAsync<SuggestionBoxStatusVO>(
                    new JsonSerializerOptions(JsonSerializerDefaults.Web), linked.Token);
                return box != null && box.Status == 0; // Active only
            }
            catch { return false; } // fail-closed (timeout/unreachable)
        }
    }
}
