using System.Net.Http.Headers;
using System.Text.Json;
using SubscriptionService.Models.ExternalDTOs;

namespace SubscriptionService.ServiceCalls
{
    // Non-fatal client for BillingNotificationService. Mirrors LoggerServiceClient.TryLogAsync:
    // try/catch-swallow + short timeout + forward the caller's Authorization header, so a
    // billing-notification failure never breaks the underlying subscription/payment operation.
    public class BillingServiceCall
    {
        private readonly HttpClient _httpClient;

        public BillingServiceCall() { }   // parameterless ctor for tests/mocking

        public BillingServiceCall(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("BillingNotificationService");
        }

        // virtual so unit tests can mock/override (fan-out count, non-fatal behaviour).
        public virtual async Task CreateBillingNotificationAsync(
            BillingNotificationCreateDTO dto,
            string? bearerHeader = null,
            CancellationToken requestCt = default)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, "/api/billingnotification");

                if (!string.IsNullOrWhiteSpace(bearerHeader))
                    req.Headers.Authorization = AuthenticationHeaderValue.Parse(bearerHeader);

                var opts = new JsonSerializerOptions(JsonSerializerDefaults.Web);
                req.Content = JsonContent.Create(dto, options: opts);

                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(800));
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(requestCt, timeoutCts.Token);

                using var res = await _httpClient.SendAsync(req, linked.Token);
            }
            catch { }   // swallow — billing-notification failure must not break the underlying op
        }
    }
}
