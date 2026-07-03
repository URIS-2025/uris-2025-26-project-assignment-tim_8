namespace McpGateway.Clients;

/// <summary>
/// Write client for SuggestionService REST endpoints (submission update + comment creation). Same
/// pattern as <see cref="ProblemServiceClient"/>. The suggestion update omits CategoryIds (the repo
/// ignores it) and carries the current Title/Description so a status-only change wipes nothing.
/// </summary>
public class SuggestionServiceClient
{
    private readonly HttpClient _http = default!;

    public SuggestionServiceClient() { } // for Moq

    public SuggestionServiceClient(IHttpClientFactory factory)
        => _http = factory.CreateClient("SuggestionService");

    public virtual Task<WriteResult> UpdateAsync(SuggestionUpdateRequest req, string? bearer, CancellationToken ct)
        => WriteClientHttp.SendAsync(_http, HttpMethod.Put, "/api/Suggestion", req, bearer, ct);

    public virtual Task<WriteResult> AddCommentAsync(SuggestionCommentRequest req, string? bearer, CancellationToken ct)
        => WriteClientHttp.SendAsync(_http, HttpMethod.Post, "/api/SuggestionComment", req, bearer, ct);
}
