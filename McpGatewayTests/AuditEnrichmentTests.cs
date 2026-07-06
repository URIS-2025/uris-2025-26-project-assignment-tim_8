using System.Security.Claims;
using System.Security.Cryptography;
using McpGateway.Audit;
using McpGateway.Authorization;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Protocol;
using Moq;
using Xunit;

namespace McpGatewayTests;

/// <summary>
/// The outcome-enrichment path (Faza C): the decision record is written BEFORE the tool runs
/// (fail-closed), then enriched in place with the execution outcome AFTER — best-effort, so an
/// enrichment failure never fails the call, while a decision-write failure still fails closed.
/// </summary>
public class AuditEnrichmentTests
{
    private const string AgentId = "ai-assistant";

    private static readonly ToolPolicy[] Tools =
    [
        new("get_problem_stats", ["Admin", "Manager"], OrgScoped: true, IsWrite: false),
    ];

    private static (ToolAuthorizationFilter Filter, RequestGatekeeper Gk, RSA Key) Build(IAuditSink sink)
    {
        var key = RSA.Create(2048);
        var agents = new[] { new AgentPolicy(AgentId, key.ExportSubjectPublicKeyInfoPem(), ["get_problem_stats"]) };
        var store = new PolicyStore(Tools, agents);
        var gk = new RequestGatekeeper(
            new AgentTokenValidator(store.Agents), new ToolAuthorizer(store.ToolPolicies), store, sink,
            NullLogger<RequestGatekeeper>.Instance);
        return (new ToolAuthorizationFilter(gk), gk, key);
    }

    private static (string Token, ClaimsPrincipal User) Caller(RSA key) =>
        (TestAuth.MintAgentToken(key, AgentId, DateTime.UtcNow.AddMinutes(5)),
         TestAuth.Principal("u1", "Manager", Guid.NewGuid()));

    private static CallToolResult Ok() => new() { Content = [new TextContentBlock { Text = "ran" }] };
    private static CallToolResult Err() =>
        new() { IsError = true, Content = [new TextContentBlock { Text = "odbijeno (status 409)" }] };

    [Fact]
    public async Task Allow_success_enriches_entry_with_Success_outcome()
    {
        var sink = new InMemoryAuditSink();
        var (filter, _, key) = Build(sink);
        var (token, user) = Caller(key);

        await filter.EvaluateAsync(token, user, "get_problem_stats", null,
            next: _ => ValueTask.FromResult(Ok()), CancellationToken.None);

        var entry = Assert.Single(sink.Entries);
        Assert.Equal(AuditOutcome.Success, entry.Outcome);
        Assert.NotNull(entry.DurationMs);
        Assert.Null(entry.Error);
    }

    [Fact]
    public async Task Allow_error_result_enriches_entry_with_Error_and_sanitized_text()
    {
        var sink = new InMemoryAuditSink();
        var (filter, _, key) = Build(sink);
        var (token, user) = Caller(key);

        await filter.EvaluateAsync(token, user, "get_problem_stats", null,
            next: _ => ValueTask.FromResult(Err()), CancellationToken.None);

        var entry = Assert.Single(sink.Entries);
        Assert.Equal(AuditOutcome.Error, entry.Outcome);
        Assert.Equal("odbijeno (status 409)", entry.Error);
    }

    [Fact]
    public async Task Enrichment_failure_does_not_fail_the_call()
    {
        // RecordAsync succeeds (decision durably written); UpdateOutcomeAsync throws AFTER execution.
        var sink = new Mock<IAuditSink>();
        sink.Setup(s => s.RecordAsync(It.IsAny<AuditEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        sink.Setup(s => s.UpdateOutcomeAsync(It.IsAny<Guid>(), It.IsAny<AuditOutcome>(), It.IsAny<int>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("audit store down"));
        var (filter, _, key) = Build(sink.Object);
        var (token, user) = Caller(key);
        var nextCalled = false;

        var result = await filter.EvaluateAsync(token, user, "get_problem_stats", null,
            next: _ => { nextCalled = true; return ValueTask.FromResult(Ok()); }, CancellationToken.None);

        Assert.True(nextCalled);
        Assert.True(result.IsError != true); // the action already ran; enrich failure is swallowed
    }

    [Fact]
    public async Task Tool_exception_is_enriched_as_Error_and_rethrown()
    {
        var sink = new InMemoryAuditSink();
        var (filter, _, key) = Build(sink);
        var (token, user) = Caller(key);

        // The tool throws → the filter must enrich the audit with Error AND rethrow (never hide it).
        await Assert.ThrowsAsync<InvalidOperationException>(() => filter.EvaluateAsync(
            token, user, "get_problem_stats", null,
            next: _ => throw new InvalidOperationException("boom"), CancellationToken.None).AsTask());

        var entry = Assert.Single(sink.Entries);
        Assert.Equal(AuditOutcome.Error, entry.Outcome);
    }

    [Fact]
    public async Task Decision_write_failure_fails_closed_with_deny_and_never_executes()
    {
        // RecordAsync (BEFORE execution) throws → the call must be DENIED (no execution without audit).
        var sink = new Mock<IAuditSink>();
        sink.Setup(s => s.RecordAsync(It.IsAny<AuditEntry>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("audit store down"));
        var (filter, gk, key) = Build(sink.Object);
        var (token, user) = Caller(key);
        var nextCalled = false;

        var decision = await gk.AuthorizeAndAuditAsync(token, user, "get_problem_stats");
        // And through the filter: the tool must never run when the decision couldn't be recorded.
        var result = await filter.EvaluateAsync(token, user, "get_problem_stats", null,
            next: _ => { nextCalled = true; return ValueTask.FromResult(Ok()); }, CancellationToken.None);

        Assert.Equal(AuthDecision.Deny, decision.Decision);
        Assert.False(nextCalled);            // no execution without a durable audit record
        Assert.True(result.IsError == true);
    }
}
