using System.Net.Http.Headers;
using System.Text.Json;

namespace SuggestionService.Clients
{
    // Resolves a suggestion box's owning OrganizationId via SuggestionBoxService.
    // Non-throwing: returns null on any failure (missing box, non-2xx, network error)
    // so a box-service outage never breaks the underlying create.
    public class SuggestionBoxServiceClient
    {
        private readonly HttpClient _http;

        public SuggestionBoxServiceClient() { }   // parameterless ctor for tests/mocking

        public SuggestionBoxServiceClient(IHttpClientFactory factory)
        {
            _http = factory.CreateClient("SuggestionBoxService");
        }

        // virtual so unit tests can mock/override without a real HttpClient
        public virtual async Task<Guid?> TryGetOrganizationIdAsync(Guid boxId, string? bearerHeader, CancellationToken requestCt)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/SuggestionBox/{boxId}");

                if (!string.IsNullOrWhiteSpace(bearerHeader))
                    req.Headers.Authorization = AuthenticationHeaderValue.Parse(bearerHeader);

                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(800));
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(requestCt, timeoutCts.Token);

                using var res = await _http.SendAsync(req, linked.Token);
                if (!res.IsSuccessStatusCode)
                    return null;

                var opts = new JsonSerializerOptions(JsonSerializerDefaults.Web);
                var box = await res.Content.ReadFromJsonAsync<SuggestionBoxVO>(opts, linked.Token);
                if (box == null || box.OrganizationId == Guid.Empty)
                    return null;

                return box.OrganizationId;
            }
            catch
            {
                return null;   // non-fatal — skip the notification
            }
        }
    }
}
