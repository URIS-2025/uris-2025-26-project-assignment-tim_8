namespace McpGateway.Clients;

/// <summary>
/// Write client for SuggestionBoxService REST endpoints (status / password / create). Same pattern as
/// <see cref="ProblemBoxServiceClient"/>: named <see cref="HttpClient"/> + OBO bearer, returns a
/// <see cref="WriteResult"/>, <c>virtual</c> methods + parameterless ctor for Moq.
/// </summary>
public class SuggestionBoxServiceClient
{
    private readonly HttpClient _http = default!;

    public SuggestionBoxServiceClient() { } // for Moq

    public SuggestionBoxServiceClient(IHttpClientFactory factory)
        => _http = factory.CreateClient("SuggestionBoxService");

    public virtual Task<WriteResult> SetStatusAsync(Guid id, int status, string? bearer, CancellationToken ct)
        => WriteClientHttp.SendAsync(_http, HttpMethod.Put, $"/api/SuggestionBox/{id}/status",
            new BoxStatusRequest(status), bearer, ct);

    public virtual Task<WriteResult> SetPasswordAsync(Guid id, string password, string? bearer, CancellationToken ct)
        => WriteClientHttp.SendAsync(_http, HttpMethod.Put, $"/api/SuggestionBox/{id}/password",
            new BoxPasswordRequest(password), bearer, ct);

    public virtual Task<WriteResult> CreateAsync(CreateBoxRequest box, string? bearer, CancellationToken ct)
        => WriteClientHttp.SendAsync(_http, HttpMethod.Post, "/api/SuggestionBox", box, bearer, ct);
}
