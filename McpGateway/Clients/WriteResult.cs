using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace McpGateway.Clients;

/// <summary>
/// Outcome of a write REST call. Unlike the fire-and-forget <c>LoggerServiceClient</c> (which
/// swallows every failure), a write call changes state, so its outcome MUST be reported back to the
/// tool — never hidden — so the agent learns whether the action succeeded, was refused, or errored.
/// </summary>
/// <param name="Success">True iff the REST endpoint returned a 2xx status.</param>
/// <param name="StatusCode">The HTTP status code; <c>0</c> means no HTTP response (transport/timeout).</param>
/// <param name="Detail">
/// A bounded, raw server detail on failure — for the gateway's own mapping/logging only. It is NOT
/// forwarded verbatim to the agent; the tool sanitizes it (a 5xx body never reaches the LLM).
/// </param>
public record WriteResult(bool Success, int StatusCode, string? Detail);

/// <summary>
/// Shared HTTP send helper for the write REST clients. Mirrors the Faza-A <c>LoggerServiceClient</c>
/// shape (named <see cref="HttpClient"/>, forwarded OBO bearer, linked timeout, System.Text.Json web
/// options) with one deliberate difference: it does NOT swallow — it returns a <see cref="WriteResult"/>
/// describing the outcome, including transport failures.
/// </summary>
internal static class WriteClientHttp
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // Writes are heavier than the 800 ms fire-and-forget log call; give the downstream more room.
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    // Bound the captured failure detail so a large error body can never blow up the audit/log line.
    private const int MaxDetail = 300;

    public static async Task<WriteResult> SendAsync(
        HttpClient http, HttpMethod method, string path, object? body,
        string? bearerHeader, CancellationToken requestCt)
    {
        try
        {
            using var req = new HttpRequestMessage(method, path);

            // Forward the caller's OBO bearer so the downstream sees the real identity (audit + any
            // future [Authorize]); enums in the body serialize as INTs (no JsonStringEnumConverter).
            if (!string.IsNullOrWhiteSpace(bearerHeader))
                req.Headers.Authorization = AuthenticationHeaderValue.Parse(bearerHeader);
            if (body is not null)
                req.Content = JsonContent.Create(body, options: Json);

            using var timeoutCts = new CancellationTokenSource(Timeout);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(requestCt, timeoutCts.Token);

            using var res = await http.SendAsync(req, linked.Token);
            var code = (int)res.StatusCode;
            if (res.IsSuccessStatusCode)
                return new WriteResult(true, code, null);

            // Non-2xx: capture a BOUNDED detail (the tool decides how much, if any, to surface).
            string? detail = null;
            try
            {
                var raw = await res.Content.ReadAsStringAsync(linked.Token);
                detail = raw.Length > MaxDetail ? raw[..MaxDetail] : raw;
            }
            catch { /* detail is best-effort — a read failure must not mask the status code */ }

            return new WriteResult(false, code, detail);
        }
        catch (Exception ex)
        {
            // NON-swallow (unlike the logger client): a transport/timeout failure is REPORTED as a
            // result the tool can surface — never hidden. StatusCode 0 = no HTTP response arrived.
            return new WriteResult(false, 0, ex.Message);
        }
    }
}
