namespace McpGateway.Clients;

/// <summary>
/// Write client for ProblemBoxService REST endpoints (status / password / create). Follows the
/// Faza-A named-<see cref="HttpClient"/> + OBO-bearer pattern but returns a <see cref="WriteResult"/>
/// instead of swallowing. Methods are <c>virtual</c> and a parameterless ctor exists so the tools
/// can be unit-tested against a Moq of this client (no real HttpClient).
/// </summary>
public class ProblemBoxServiceClient
{
    private readonly HttpClient _http = default!;

    public ProblemBoxServiceClient() { } // for Moq

    public ProblemBoxServiceClient(IHttpClientFactory factory)
        => _http = factory.CreateClient("ProblemBoxService");

    public virtual Task<WriteResult> SetStatusAsync(Guid id, int status, string? bearer, CancellationToken ct)
        => WriteClientHttp.SendAsync(_http, HttpMethod.Put, $"/api/ProblemBox/{id}/status",
            new BoxStatusRequest(status), bearer, ct);

    public virtual Task<WriteResult> SetPasswordAsync(Guid id, string password, string? bearer, CancellationToken ct)
        => WriteClientHttp.SendAsync(_http, HttpMethod.Put, $"/api/ProblemBox/{id}/password",
            new BoxPasswordRequest(password), bearer, ct);

    public virtual Task<WriteResult> CreateAsync(CreateBoxRequest box, string? bearer, CancellationToken ct)
        => WriteClientHttp.SendAsync(_http, HttpMethod.Post, "/api/ProblemBox", box, bearer, ct);
}
