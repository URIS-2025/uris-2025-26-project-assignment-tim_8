using System.Security.Cryptography;
using System.Text;

namespace OrganizationService.Clients;

public interface IPwnedPasswordsClient
{
    Task<bool> IsBreachedAsync(string password, CancellationToken ct);
}

public class PwnedPasswordsClient : IPwnedPasswordsClient
{
    private readonly IHttpClientFactory _factory;

    public PwnedPasswordsClient(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    public async Task<bool> IsBreachedAsync(string password, CancellationToken ct)
    {
        try
        {
            var hash = ComputeSha1UpperHex(password);
            var prefix = hash.Substring(0, 5);
            var suffix = hash.Substring(5);

            var client = _factory.CreateClient("PwnedPasswords");

            using var req = new HttpRequestMessage(HttpMethod.Get, $"range/{prefix}");

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(2000));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            using var res = await client.SendAsync(req, linked.Token);
            res.EnsureSuccessStatusCode();

            var body = await res.Content.ReadAsStringAsync(linked.Token);

            foreach (var line in body.Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0)
                    continue;

                var sep = trimmed.IndexOf(':');
                var candidate = sep >= 0 ? trimmed.Substring(0, sep) : trimmed;

                if (string.Equals(candidate, suffix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
        catch
        {
            // Fail open: any network/HTTP error is treated as not breached.
            return false;
        }
    }

    private static string ComputeSha1UpperHex(string password)
    {
        var bytes = SHA1.HashData(Encoding.UTF8.GetBytes(password ?? string.Empty));
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
            sb.Append(b.ToString("X2"));
        return sb.ToString();
    }
}
