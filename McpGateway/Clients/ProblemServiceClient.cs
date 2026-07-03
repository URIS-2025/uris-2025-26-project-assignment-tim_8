namespace McpGateway.Clients;

/// <summary>
/// Write client for ProblemService REST endpoints (submission update + comment creation). Named
/// <see cref="HttpClient"/> + OBO bearer, returns a <see cref="WriteResult"/>, <c>virtual</c> methods
/// + parameterless ctor for Moq. The submission update is a full-DTO PUT (the tool builds it via
/// read-modify-write so it never wipes the fields it isn't changing).
/// </summary>
public class ProblemServiceClient
{
    private readonly HttpClient _http = default!;

    public ProblemServiceClient() { } // for Moq

    public ProblemServiceClient(IHttpClientFactory factory)
        => _http = factory.CreateClient("ProblemService");

    public virtual Task<WriteResult> UpdateAsync(ProblemUpdateRequest req, string? bearer, CancellationToken ct)
        => WriteClientHttp.SendAsync(_http, HttpMethod.Put, "/api/Problem", req, bearer, ct);

    public virtual Task<WriteResult> AddCommentAsync(ProblemCommentRequest req, string? bearer, CancellationToken ct)
        => WriteClientHttp.SendAsync(_http, HttpMethod.Post, "/api/ProblemComment", req, bearer, ct);
}
