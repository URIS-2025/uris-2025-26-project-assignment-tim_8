using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace OrganizationService.Clients;

public interface ICaptchaVerifierClient
{
    Task<bool> VerifyAsync(string? token, CancellationToken ct);
}

public class CaptchaVerifierClient : ICaptchaVerifierClient
{
    private readonly IHttpClientFactory _factory;
    private readonly IConfiguration _configuration;

    public CaptchaVerifierClient(IHttpClientFactory factory, IConfiguration configuration)
    {
        _factory = factory;
        _configuration = configuration;
    }

    public async Task<bool> VerifyAsync(string? token, CancellationToken ct)
    {
        // Fail closed: no token means the widget was never solved → not verified.
        if (string.IsNullOrWhiteSpace(token))
            return false;

        try
        {
            var secret = _configuration["Captcha:SecretKey"];
            if (string.IsNullOrWhiteSpace(secret))
                return false; // misconfigured server → fail closed, never let traffic through unverified

            var client = _factory.CreateClient("Turnstile");

            using var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("secret", secret),
                new KeyValuePair<string, string>("response", token),
            });

            using var req = new HttpRequestMessage(HttpMethod.Post, "turnstile/v0/siteverify")
            {
                Content = content
            };

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(2000));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            using var res = await client.SendAsync(req, linked.Token);
            if (!res.IsSuccessStatusCode)
                return false;

            var body = await res.Content.ReadAsStringAsync(linked.Token);

            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.TryGetProperty("success", out var success)
                && success.ValueKind == JsonValueKind.True;
        }
        catch
        {
            // Fail closed: any network / HTTP / parse error is treated as "not verified".
            return false;
        }
    }
}
