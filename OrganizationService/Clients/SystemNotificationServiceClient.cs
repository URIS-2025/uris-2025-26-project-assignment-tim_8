using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace OrganizationService.Clients
{
    // Non-fatal client for SystemNotificationService. Mirrors LoggerServiceClient:
    // try/catch-swallow + short timeout so a notification failure never breaks the
    // underlying user/role operation.
    public class SystemNotificationServiceClient
    {
        private readonly HttpClient _http;

        public SystemNotificationServiceClient() { }   // parameterless ctor for tests/mocking

        public SystemNotificationServiceClient(IHttpClientFactory factory)
        {
            _http = factory.CreateClient("SystemNotificationService");
        }

        // virtual so unit tests can mock/override without a real HttpClient
        public virtual async Task TryNotifyAsync(SystemNotificationCreationDTO dto, string? bearerHeader, CancellationToken requestCt)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, "/api/SystemNotification");

                if (!string.IsNullOrWhiteSpace(bearerHeader))
                    req.Headers.Authorization = AuthenticationHeaderValue.Parse(bearerHeader);

                var opts = new JsonSerializerOptions(JsonSerializerDefaults.Web);
                req.Content = JsonContent.Create(dto, options: opts);

                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(800));
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(requestCt, timeoutCts.Token);

                using var res = await _http.SendAsync(req, linked.Token);
            }
            catch { }   // swallow — notification failure must not break the underlying op
        }
    }
}
