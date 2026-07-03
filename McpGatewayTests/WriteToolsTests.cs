using System.Text.Json;
using McpGateway.Authorization;
using McpGateway.Clients;
using McpGateway.Data;
using McpGateway.Tools;
using ModelContextProtocol.Protocol;
using Moq;
using Xunit;

namespace McpGatewayTests;

/// <summary>
/// Write-tool bodies with mocked REST clients + <see cref="IReadRepository"/> (EF InMemory can't run
/// the views or the REST endpoints). These prove the security-critical orchestration: org ownership
/// is verified BEFORE any REST call (a manager can never touch a foreign box/submission), create_box
/// forces the org, the read-modify-write preserves untouched fields, REST failures surface as errors
/// (never thrown/swallowed), and the OBO bearer + author identity come from the invocation context.
/// </summary>
public class WriteToolsTests
{
    private const string Bearer = "Bearer obo-token";

    private static IInvocationContextAccessor Ctx(Guid? effectiveOrg, string role, string userId, string? bearer = Bearer) =>
        new InvocationContextAccessor { Current = new InvocationContext(effectiveOrg, role, userId, bearer) };

    private static string TextOf(CallToolResult r) =>
        string.Concat(r.Content.OfType<TextContentBlock>().Select(c => c.Text));

    // ── set_box_status ───────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SetBoxStatus_manager_in_scope_calls_rest_with_obo_bearer_and_reports_success()
    {
        var org = Guid.NewGuid();
        var boxId = Guid.NewGuid();
        var repo = new Mock<IReadRepository>(MockBehavior.Loose);
        repo.Setup(r => r.GetProblemBoxIdsAsync(org, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { boxId } as IReadOnlyList<Guid>);

        var pBox = new Mock<ProblemBoxServiceClient>();
        pBox.Setup(c => c.SetStatusAsync(boxId, 1, Bearer, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WriteResult(true, 204, null));
        var sBox = new Mock<SuggestionBoxServiceClient>();

        var result = await WriteTools.SetBoxStatus(
            pBox.Object, sBox.Object, repo.Object, Ctx(org, "Manager", "u1"),
            type: "problem", id: boxId, status: 1);

        Assert.False(result.IsError);
        pBox.Verify(c => c.SetStatusAsync(boxId, 1, Bearer, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetBoxStatus_manager_out_of_scope_denies_before_calling_rest()
    {
        var org = Guid.NewGuid();
        var foreignBox = Guid.NewGuid();
        var repo = new Mock<IReadRepository>(MockBehavior.Loose);
        // The manager's org owns a DIFFERENT box — the target is not in scope.
        repo.Setup(r => r.GetProblemBoxIdsAsync(org, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { Guid.NewGuid() } as IReadOnlyList<Guid>);

        var pBox = new Mock<ProblemBoxServiceClient>(MockBehavior.Strict); // ANY call = test failure
        var sBox = new Mock<SuggestionBoxServiceClient>(MockBehavior.Strict);

        var result = await WriteTools.SetBoxStatus(
            pBox.Object, sBox.Object, repo.Object, Ctx(org, "Manager", "u1"),
            type: "problem", id: foreignBox, status: 1);

        Assert.True(result.IsError);
        Assert.Contains("zabranjeno", TextOf(result));
        pBox.Verify(c => c.SetStatusAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetBoxStatus_admin_skips_membership_check_and_calls_rest()
    {
        var boxId = Guid.NewGuid();
        var repo = new Mock<IReadRepository>(MockBehavior.Strict); // no box-id lookup expected for admin
        var pBox = new Mock<ProblemBoxServiceClient>();
        pBox.Setup(c => c.SetStatusAsync(boxId, 0, Bearer, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WriteResult(true, 204, null));
        var sBox = new Mock<SuggestionBoxServiceClient>();

        var result = await WriteTools.SetBoxStatus(
            pBox.Object, sBox.Object, repo.Object, Ctx(null, "Admin", "admin1"),
            type: "problem", id: boxId, status: 0);

        Assert.False(result.IsError);
        repo.Verify(r => r.GetProblemBoxIdsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        pBox.Verify(c => c.SetStatusAsync(boxId, 0, Bearer, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetBoxStatus_rest_409_surfaces_conflict_as_error()
    {
        var org = Guid.NewGuid();
        var boxId = Guid.NewGuid();
        var repo = new Mock<IReadRepository>(MockBehavior.Loose);
        repo.Setup(r => r.GetProblemBoxIdsAsync(org, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { boxId } as IReadOnlyList<Guid>);
        var pBox = new Mock<ProblemBoxServiceClient>();
        pBox.Setup(c => c.SetStatusAsync(boxId, 1, Bearer, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WriteResult(false, 409, "Box is inactive"));
        var sBox = new Mock<SuggestionBoxServiceClient>();

        var result = await WriteTools.SetBoxStatus(
            pBox.Object, sBox.Object, repo.Object, Ctx(org, "Manager", "u1"),
            type: "problem", id: boxId, status: 1);

        Assert.True(result.IsError);
        Assert.Contains("sukob stanja", TextOf(result));
        Assert.DoesNotContain("Box is inactive", TextOf(result)); // raw server detail never leaked
    }

    [Fact]
    public async Task SetBoxStatus_unknown_type_is_error_and_calls_nothing()
    {
        var repo = new Mock<IReadRepository>(MockBehavior.Strict);
        var pBox = new Mock<ProblemBoxServiceClient>(MockBehavior.Strict);
        var sBox = new Mock<SuggestionBoxServiceClient>(MockBehavior.Strict);

        var result = await WriteTools.SetBoxStatus(
            pBox.Object, sBox.Object, repo.Object, Ctx(Guid.NewGuid(), "Manager", "u1"),
            type: "banana", id: Guid.NewGuid(), status: 1);

        Assert.True(result.IsError);
    }

    [Fact]
    public async Task SetBoxStatus_missing_context_fails_closed()
    {
        var repo = new Mock<IReadRepository>(MockBehavior.Loose);
        var noCtx = new InvocationContextAccessor(); // Current == null

        await Assert.ThrowsAsync<InvalidOperationException>(() => WriteTools.SetBoxStatus(
            new Mock<ProblemBoxServiceClient>().Object, new Mock<SuggestionBoxServiceClient>().Object,
            repo.Object, noCtx, type: "problem", id: Guid.NewGuid(), status: 1));
    }

    // ── set_box_password ───────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SetBoxPassword_suggestion_manager_in_scope_forwards_password_and_bearer()
    {
        var org = Guid.NewGuid();
        var boxId = Guid.NewGuid();
        var repo = new Mock<IReadRepository>(MockBehavior.Loose);
        repo.Setup(r => r.GetSuggestionBoxIdsAsync(org, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { boxId } as IReadOnlyList<Guid>);

        var pBox = new Mock<ProblemBoxServiceClient>(MockBehavior.Strict);
        var sBox = new Mock<SuggestionBoxServiceClient>();
        sBox.Setup(c => c.SetPasswordAsync(boxId, "s3cret", Bearer, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WriteResult(true, 204, null));

        var result = await WriteTools.SetBoxPassword(
            pBox.Object, sBox.Object, repo.Object, Ctx(org, "Manager", "u1"),
            type: "suggestion", id: boxId, password: "s3cret");

        Assert.False(result.IsError);
        sBox.Verify(c => c.SetPasswordAsync(boxId, "s3cret", Bearer, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetBoxPassword_manager_out_of_scope_denies_before_calling_rest()
    {
        var org = Guid.NewGuid();
        var repo = new Mock<IReadRepository>(MockBehavior.Loose);
        repo.Setup(r => r.GetSuggestionBoxIdsAsync(org, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Guid>() as IReadOnlyList<Guid>);
        var pBox = new Mock<ProblemBoxServiceClient>(MockBehavior.Strict);
        var sBox = new Mock<SuggestionBoxServiceClient>(MockBehavior.Strict);

        var result = await WriteTools.SetBoxPassword(
            pBox.Object, sBox.Object, repo.Object, Ctx(org, "Manager", "u1"),
            type: "suggestion", id: Guid.NewGuid(), password: "s3cret");

        Assert.True(result.IsError);
        sBox.Verify(c => c.SetPasswordAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── create_box ─────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateBox_manager_forces_own_org_ignoring_llm_supplied_org()
    {
        var own = Guid.NewGuid();
        var attackerOrg = Guid.NewGuid();
        var pBox = new Mock<ProblemBoxServiceClient>();
        pBox.Setup(c => c.CreateAsync(It.IsAny<CreateBoxRequest>(), Bearer, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WriteResult(true, 201, null));
        var sBox = new Mock<SuggestionBoxServiceClient>(MockBehavior.Strict);

        var result = await WriteTools.CreateBox(
            pBox.Object, sBox.Object, Ctx(own, "Manager", "mgr1"),
            type: "problem", name: "Box A", description: "desc",
            isDarkTheme: true, password: "pw", organizationId: attackerOrg);

        Assert.False(result.IsError);
        // The LLM-supplied org is ignored; the box is created in the manager's OWN org, author = OBO user.
        pBox.Verify(c => c.CreateAsync(
            It.Is<CreateBoxRequest>(b => b.OrganizationId == own && b.OrganizationId != attackerOrg
                                         && b.CreatedBy == "mgr1" && b.Name == "Box A"),
            Bearer, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateBox_admin_uses_supplied_org()
    {
        var target = Guid.NewGuid();
        var pBox = new Mock<ProblemBoxServiceClient>(MockBehavior.Strict);
        var sBox = new Mock<SuggestionBoxServiceClient>();
        sBox.Setup(c => c.CreateAsync(It.IsAny<CreateBoxRequest>(), Bearer, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WriteResult(true, 201, null));

        var result = await WriteTools.CreateBox(
            pBox.Object, sBox.Object, Ctx(null, "Admin", "admin1"),
            type: "suggestion", name: "Box B", description: "desc", organizationId: target);

        Assert.False(result.IsError);
        sBox.Verify(c => c.CreateAsync(
            It.Is<CreateBoxRequest>(b => b.OrganizationId == target && b.CreatedBy == "admin1"),
            Bearer, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateBox_admin_without_org_is_error_and_calls_nothing()
    {
        var pBox = new Mock<ProblemBoxServiceClient>(MockBehavior.Strict);
        var sBox = new Mock<SuggestionBoxServiceClient>(MockBehavior.Strict);

        var result = await WriteTools.CreateBox(
            pBox.Object, sBox.Object, Ctx(null, "Admin", "admin1"),
            type: "problem", name: "Box C", description: "desc", organizationId: null);

        Assert.True(result.IsError);
        Assert.Contains("organizationId", TextOf(result));
    }

    // ── add_comment ────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddComment_problem_manager_in_scope_posts_with_obo_author_not_anonymous()
    {
        var org = Guid.NewGuid();
        var boxId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var repo = new Mock<IReadRepository>(MockBehavior.Loose);
        repo.Setup(r => r.GetProblemBoxIdsAsync(org, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { boxId } as IReadOnlyList<Guid>);
        repo.Setup(r => r.GetProblemUpdateFieldsAsync(submissionId, It.IsAny<IReadOnlyList<Guid>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProblemUpdateFieldsDto("T", "D", boxId, 0, 0));

        var problems = new Mock<ProblemServiceClient>();
        problems.Setup(c => c.AddCommentAsync(It.IsAny<ProblemCommentRequest>(), Bearer, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WriteResult(true, 201, null));
        var suggestions = new Mock<SuggestionServiceClient>(MockBehavior.Strict);

        var result = await WriteTools.AddComment(
            problems.Object, suggestions.Object, repo.Object, Ctx(org, "Manager", userId.ToString()),
            type: "problem", id: submissionId, text: "hello");

        Assert.False(result.IsError);
        problems.Verify(c => c.AddCommentAsync(
            It.Is<ProblemCommentRequest>(r => r.ProblemId == submissionId && r.CommentText == "hello"
                                              && r.ProblemCommentAuthorId == userId && r.IsAnonymous == false),
            Bearer, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddComment_manager_out_of_scope_denies_before_posting()
    {
        var org = Guid.NewGuid();
        var repo = new Mock<IReadRepository>(MockBehavior.Loose);
        repo.Setup(r => r.GetProblemBoxIdsAsync(org, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { Guid.NewGuid() } as IReadOnlyList<Guid>);
        // Out-of-scope / missing submission → the RMW-source read returns null.
        repo.Setup(r => r.GetProblemUpdateFieldsAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyList<Guid>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProblemUpdateFieldsDto?)null);
        var problems = new Mock<ProblemServiceClient>(MockBehavior.Strict);
        var suggestions = new Mock<SuggestionServiceClient>(MockBehavior.Strict);

        var result = await WriteTools.AddComment(
            problems.Object, suggestions.Object, repo.Object, Ctx(org, "Manager", Guid.NewGuid().ToString()),
            type: "problem", id: Guid.NewGuid(), text: "hi");

        Assert.True(result.IsError);
        problems.Verify(c => c.AddCommentAsync(It.IsAny<ProblemCommentRequest>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddComment_suggestion_admin_skips_verify_and_sets_createdby_to_obo_user()
    {
        var userId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        var repo = new Mock<IReadRepository>(MockBehavior.Strict); // admin → no scope reads
        var problems = new Mock<ProblemServiceClient>(MockBehavior.Strict);
        var suggestions = new Mock<SuggestionServiceClient>();
        suggestions.Setup(c => c.AddCommentAsync(It.IsAny<SuggestionCommentRequest>(), Bearer, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WriteResult(true, 201, null));

        var result = await WriteTools.AddComment(
            problems.Object, suggestions.Object, repo.Object, Ctx(null, "Admin", userId.ToString()),
            type: "suggestion", id: submissionId, text: "note");

        Assert.False(result.IsError);
        suggestions.Verify(c => c.AddCommentAsync(
            It.Is<SuggestionCommentRequest>(r => r.SuggestionId == submissionId && r.Text == "note"
                                                 && r.CommentAuthorId == userId && r.CreatedBy == userId.ToString()
                                                 && r.IsAnonymous == false),
            Bearer, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddComment_non_guid_userid_is_error_and_posts_nothing()
    {
        var repo = new Mock<IReadRepository>(MockBehavior.Loose);
        var problems = new Mock<ProblemServiceClient>(MockBehavior.Strict);
        var suggestions = new Mock<SuggestionServiceClient>(MockBehavior.Strict);

        var result = await WriteTools.AddComment(
            problems.Object, suggestions.Object, repo.Object, Ctx(null, "Admin", "not-a-guid"),
            type: "problem", id: Guid.NewGuid(), text: "hi");

        Assert.True(result.IsError);
    }

    // ── update_submission_status ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateSubmissionStatus_problem_read_modify_write_preserves_other_fields()
    {
        var org = Guid.NewGuid();
        var boxId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        var repo = new Mock<IReadRepository>(MockBehavior.Loose);
        repo.Setup(r => r.GetProblemBoxIdsAsync(org, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { boxId } as IReadOnlyList<Guid>);
        // Current values: Title/Description/Priority must be echoed back unchanged; only Status flips.
        repo.Setup(r => r.GetProblemUpdateFieldsAsync(submissionId, It.IsAny<IReadOnlyList<Guid>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProblemUpdateFieldsDto("Orig title", "Orig desc", boxId, Priority: 2, Status: 0));

        var problems = new Mock<ProblemServiceClient>();
        problems.Setup(c => c.UpdateAsync(It.IsAny<ProblemUpdateRequest>(), Bearer, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WriteResult(true, 200, null));
        var suggestions = new Mock<SuggestionServiceClient>(MockBehavior.Strict);

        var result = await WriteTools.UpdateSubmissionStatus(
            problems.Object, suggestions.Object, repo.Object, Ctx(org, "Manager", "u1"),
            type: "problem", id: submissionId, status: 1);

        Assert.False(result.IsError);
        problems.Verify(c => c.UpdateAsync(
            It.Is<ProblemUpdateRequest>(r => r.Id == submissionId && r.Status == 1        // only Status changed
                                             && r.Title == "Orig title" && r.Description == "Orig desc"
                                             && r.ProblemBoxId == boxId && r.Priority == 2), // preserved
            Bearer, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateSubmissionStatus_manager_out_of_scope_denies_before_writing()
    {
        var org = Guid.NewGuid();
        var repo = new Mock<IReadRepository>(MockBehavior.Loose);
        repo.Setup(r => r.GetProblemBoxIdsAsync(org, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { Guid.NewGuid() } as IReadOnlyList<Guid>);
        repo.Setup(r => r.GetProblemUpdateFieldsAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyList<Guid>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProblemUpdateFieldsDto?)null); // not found / out of scope
        var problems = new Mock<ProblemServiceClient>(MockBehavior.Strict);
        var suggestions = new Mock<SuggestionServiceClient>(MockBehavior.Strict);

        var result = await WriteTools.UpdateSubmissionStatus(
            problems.Object, suggestions.Object, repo.Object, Ctx(org, "Manager", "u1"),
            type: "problem", id: Guid.NewGuid(), status: 1);

        Assert.True(result.IsError);
        problems.Verify(c => c.UpdateAsync(It.IsAny<ProblemUpdateRequest>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateSubmissionStatus_suggestion_admin_writes_full_dto_with_current_title_desc()
    {
        var submissionId = Guid.NewGuid();
        var repo = new Mock<IReadRepository>(MockBehavior.Loose);
        // Admin (no org) → boxIds null; the fields read is unscoped.
        repo.Setup(r => r.GetSuggestionUpdateFieldsAsync(submissionId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuggestionUpdateFieldsDto("S title", "S desc", Status: 0));
        var problems = new Mock<ProblemServiceClient>(MockBehavior.Strict);
        var suggestions = new Mock<SuggestionServiceClient>();
        suggestions.Setup(c => c.UpdateAsync(It.IsAny<SuggestionUpdateRequest>(), Bearer, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WriteResult(true, 200, null));

        var result = await WriteTools.UpdateSubmissionStatus(
            problems.Object, suggestions.Object, repo.Object, Ctx(null, "Admin", "admin1"),
            type: "suggestion", id: submissionId, status: 1);

        Assert.False(result.IsError);
        suggestions.Verify(c => c.UpdateAsync(
            It.Is<SuggestionUpdateRequest>(r => r.Id == submissionId && r.Status == 1
                                                && r.Title == "S title" && r.Description == "S desc"),
            Bearer, It.IsAny<CancellationToken>()), Times.Once);
    }
}
