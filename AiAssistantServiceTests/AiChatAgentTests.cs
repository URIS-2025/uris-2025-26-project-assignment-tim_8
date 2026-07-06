using System.Text.Json;
using System.Text.Json.Nodes;
using AiAssistantService.Agent;
using AiAssistantService.Auth;
using AiAssistantService.Clients;
using AiAssistantService.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace AiAssistantServiceTests;

public class AiChatAgentTests
{
    private static ToolDefinition ReadTool(string name) =>
        new(name, "desc", JsonDocument.Parse("{\"type\":\"object\"}").RootElement);

    private static AgentOptions Options() => new()
    {
        Model = "claude-sonnet-5",
        MaxTokens = 4096,
        MaxIterations = 8,
        ReadTools = new[]
        {
            "get_org_overview", "get_problem_stats", "get_suggestion_stats", "list_boxes",
            "search_submissions", "get_submission_details", "get_global_stats",
        },
    };

    private static ClaudeToolCall Call(string id, string name) =>
        new(id, name, JsonNode.Parse("{}")!);

    private static ClaudeToolCall Call(string id, string name, string inputJson) =>
        new(id, name, JsonNode.Parse(inputJson)!);

    private static AiChatAgent Build(
        Mock<IClaudeAgentClient> claude,
        Mock<IMcpGatewayClient> gateway,
        Mock<IAgentTokenService>? tokens = null,
        AgentOptions? options = null)
    {
        tokens ??= new Mock<IAgentTokenService>();
        tokens.Setup(t => t.CreateAgentToken()).Returns("agent-token");
        return new AiChatAgent(claude.Object, gateway.Object, tokens.Object,
            Microsoft.Extensions.Options.Options.Create(options ?? Options()), NullLogger<AiChatAgent>.Instance);
    }

    [Fact] // T2 — read flow: Claude asks for a read tool, gateway runs it, Claude finishes
    public async Task Read_flow_executes_tool_via_gateway_and_returns_final_text()
    {
        var claude = new Mock<IClaudeAgentClient>();
        var gateway = new Mock<IMcpGatewayClient>();

        gateway.Setup(g => g.ListToolsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { ReadTool("get_org_overview") });

        // 1st Claude turn wants the read tool; 2nd turn is the final answer.
        claude.SetupSequence(c => c.SendAsync(It.IsAny<IReadOnlyList<ChatMessageDTO>>(), It.IsAny<IReadOnlyList<ToolDefinition>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClaudeResult(null, new[] { Call("tu1", "get_org_overview") }))
            .ReturnsAsync(new ClaudeResult("Vasa organizacija ima 3 kutije.", Array.Empty<ClaudeToolCall>()));

        gateway.Setup(g => g.CallToolAsync("get_org_overview", It.IsAny<JsonNode>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ToolCallResult("{\"boxes\":3}", false));

        var agent = Build(claude, gateway);

        var result = await agent.RunAsync(
            new AiChatRequestDTO { Message = "daj mi pregled organizacije" }, "Bearer user-token", CancellationToken.None);

        Assert.Equal(AiChatStatus.Completed, result.Status);
        Assert.Equal("Vasa organizacija ima 3 kutije.", result.AssistantText);
        Assert.False(string.IsNullOrWhiteSpace(result.CorrelationId));
        gateway.Verify(g => g.CallToolAsync("get_org_overview", It.IsAny<JsonNode>(),
            "Bearer user-token", "agent-token", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact] // T3 — iteration cap: a tool-looping model is force-stopped at MaxIterations
    public async Task Loop_is_bounded_by_the_iteration_cap()
    {
        var claude = new Mock<IClaudeAgentClient>();
        var gateway = new Mock<IMcpGatewayClient>();

        gateway.Setup(g => g.ListToolsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { ReadTool("get_org_overview") });

        // Claude keeps asking for the read tool forever.
        claude.Setup(c => c.SendAsync(It.IsAny<IReadOnlyList<ChatMessageDTO>>(), It.IsAny<IReadOnlyList<ToolDefinition>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClaudeResult(null, new[] { Call("tu", "get_org_overview") }));

        gateway.Setup(g => g.CallToolAsync(It.IsAny<string>(), It.IsAny<JsonNode>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ToolCallResult("{}", false));

        var agent = Build(claude, gateway);

        var result = await agent.RunAsync(
            new AiChatRequestDTO { Message = "petljaj" }, "Bearer user-token", CancellationToken.None);

        Assert.Equal(AiChatStatus.Completed, result.Status);
        Assert.Equal(8, result.Iterations);
        claude.Verify(c => c.SendAsync(It.IsAny<IReadOnlyList<ChatMessageDTO>>(), It.IsAny<IReadOnlyList<ToolDefinition>>(), It.IsAny<CancellationToken>()), Times.Exactly(8));
        gateway.Verify(g => g.CallToolAsync(It.IsAny<string>(), It.IsAny<JsonNode>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(8));
    }

    [Fact] // T4 — a write is PROPOSED (never auto-executed) and its secret args are scrubbed
    public async Task Write_tool_is_proposed_not_executed_and_args_are_scrubbed()
    {
        var claude = new Mock<IClaudeAgentClient>();
        var gateway = new Mock<IMcpGatewayClient>();

        gateway.Setup(g => g.ListToolsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { ReadTool("set_box_password") });

        claude.Setup(c => c.SendAsync(It.IsAny<IReadOnlyList<ChatMessageDTO>>(), It.IsAny<IReadOnlyList<ToolDefinition>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClaudeResult("Postavicu lozinku.",
                new[] { Call("tu1", "set_box_password", "{\"type\":\"problem\",\"id\":\"b1\",\"password\":\"secret123\"}") }));

        var agent = Build(claude, gateway);

        var result = await agent.RunAsync(
            new AiChatRequestDTO { Message = "postavi lozinku kutije" }, "Bearer user-token", CancellationToken.None);

        Assert.Equal(AiChatStatus.PendingConfirmation, result.Status);
        Assert.NotNull(result.Proposal);
        Assert.Equal("set_box_password", result.Proposal!.ToolName);
        Assert.Equal("tu1", result.Proposal.ToolUseId);
        Assert.DoesNotContain("secret123", result.Proposal.ArgsSummary); // password never surfaced

        // The gateway write was NOT invoked — human confirmation is required first.
        gateway.Verify(g => g.CallToolAsync(It.IsAny<string>(), It.IsAny<JsonNode>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact] // CRITICAL-1 fix — a tool NOT on the read allow-list (e.g. a new gateway tool) is proposed, never auto-run
    public async Task Unknown_tool_not_on_read_allowlist_is_proposed_not_executed()
    {
        var claude = new Mock<IClaudeAgentClient>();
        var gateway = new Mock<IMcpGatewayClient>();

        gateway.Setup(g => g.ListToolsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { ReadTool("delete_box") });
        claude.Setup(c => c.SendAsync(It.IsAny<IReadOnlyList<ChatMessageDTO>>(), It.IsAny<IReadOnlyList<ToolDefinition>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClaudeResult(null, new[] { Call("tu1", "delete_box", "{\"id\":\"b1\"}") }));

        var agent = Build(claude, gateway);

        var result = await agent.RunAsync(
            new AiChatRequestDTO { Message = "obrisi kutiju" }, "Bearer user-token", CancellationToken.None);

        Assert.Equal(AiChatStatus.PendingConfirmation, result.Status);
        Assert.Equal("delete_box", result.Proposal!.ToolName);
        gateway.Verify(g => g.CallToolAsync(It.IsAny<string>(), It.IsAny<JsonNode>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // History that ends with an assistant turn proposing a write (as a prior pending response would).
    private static List<ChatMessageDTO> PendingWriteHistory(string toolUseId, string toolName, string inputJson) => new()
    {
        new ChatMessageDTO { Role = "user", Content = { new ChatContentDTO { Type = "text", Text = "promeni status" } } },
        new ChatMessageDTO
        {
            Role = "assistant",
            Content =
            {
                new ChatContentDTO { Type = "tool_use", ToolUseId = toolUseId, ToolName = toolName, Input = JsonNode.Parse(inputJson) },
            },
        },
    };

    [Fact] // T5a — confirming a proposed write executes it through the gateway, then the loop continues
    public async Task Confirmed_write_is_executed_through_the_gateway()
    {
        var claude = new Mock<IClaudeAgentClient>();
        var gateway = new Mock<IMcpGatewayClient>();

        gateway.Setup(g => g.ListToolsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { ReadTool("set_box_status") });
        gateway.Setup(g => g.CallToolAsync("set_box_status", It.IsAny<JsonNode>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ToolCallResult("{\"ok\":true}", false));
        claude.Setup(c => c.SendAsync(It.IsAny<IReadOnlyList<ChatMessageDTO>>(), It.IsAny<IReadOnlyList<ToolDefinition>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClaudeResult("Status je promenjen.", Array.Empty<ClaudeToolCall>()));

        var agent = Build(claude, gateway);

        var result = await agent.RunAsync(new AiChatRequestDTO
        {
            History = PendingWriteHistory("tu1", "set_box_status", "{\"type\":\"problem\",\"id\":\"b1\",\"status\":1}"),
            PendingConfirmation = new PendingConfirmationDTO { ToolUseId = "tu1", Approved = true },
            CorrelationId = "corr-1",
        }, "Bearer user-token", CancellationToken.None);

        Assert.Equal(AiChatStatus.Completed, result.Status);
        Assert.Equal("Status je promenjen.", result.AssistantText);
        Assert.Equal("corr-1", result.CorrelationId); // correlation preserved across the confirm hop
        gateway.Verify(g => g.CallToolAsync("set_box_status", It.IsAny<JsonNode>(),
            "Bearer user-token", "agent-token", "corr-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact] // T5b — rejecting a proposed write does NOT execute it; the loop continues with the rejection
    public async Task Rejected_write_is_not_executed()
    {
        var claude = new Mock<IClaudeAgentClient>();
        var gateway = new Mock<IMcpGatewayClient>();

        gateway.Setup(g => g.ListToolsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { ReadTool("set_box_status") });
        claude.Setup(c => c.SendAsync(It.IsAny<IReadOnlyList<ChatMessageDTO>>(), It.IsAny<IReadOnlyList<ToolDefinition>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClaudeResult("U redu, necu menjati status.", Array.Empty<ClaudeToolCall>()));

        var agent = Build(claude, gateway);

        var result = await agent.RunAsync(new AiChatRequestDTO
        {
            History = PendingWriteHistory("tu1", "set_box_status", "{\"type\":\"problem\",\"id\":\"b1\",\"status\":1}"),
            PendingConfirmation = new PendingConfirmationDTO { ToolUseId = "tu1", Approved = false },
        }, "Bearer user-token", CancellationToken.None);

        Assert.Equal(AiChatStatus.Completed, result.Status);
        gateway.Verify(g => g.CallToolAsync(It.IsAny<string>(), It.IsAny<JsonNode>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact] // T7 — a supplied correlation id is forwarded to the gateway; a new one is minted when absent
    public async Task Correlation_id_is_forwarded_when_supplied_and_generated_when_absent()
    {
        var claude = new Mock<IClaudeAgentClient>();
        var gateway = new Mock<IMcpGatewayClient>();
        gateway.Setup(g => g.ListToolsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ToolDefinition>());
        claude.Setup(c => c.SendAsync(It.IsAny<IReadOnlyList<ChatMessageDTO>>(), It.IsAny<IReadOnlyList<ToolDefinition>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClaudeResult("gotovo", Array.Empty<ClaudeToolCall>()));
        var agent = Build(claude, gateway);

        var supplied = await agent.RunAsync(new AiChatRequestDTO { Message = "x", CorrelationId = "abc-123" }, "Bearer u", CancellationToken.None);
        Assert.Equal("abc-123", supplied.CorrelationId);
        gateway.Verify(g => g.ListToolsAsync("Bearer u", "agent-token", "abc-123", It.IsAny<CancellationToken>()), Times.Once);

        var generated = await agent.RunAsync(new AiChatRequestDTO { Message = "x" }, "Bearer u", CancellationToken.None);
        Assert.False(string.IsNullOrWhiteSpace(generated.CorrelationId));
        Assert.NotEqual("abc-123", generated.CorrelationId);
    }
}
