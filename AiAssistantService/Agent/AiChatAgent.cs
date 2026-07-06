using System.Text.Json.Nodes;
using AiAssistantService.Auth;
using AiAssistantService.Clients;
using AiAssistantService.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiAssistantService.Agent;

/// <inheritdoc />
public class AiChatAgent : IAiChatAgent
{
    // Argument keys safe to surface verbatim in a proposal; everything else (password, name, text,
    // free-form content) is masked so a proposal never becomes a secret/PII leak. Mirrors the
    // gateway's deny-by-default audit scrubbing.
    private static readonly HashSet<string> SafeArgKeys =
        new(StringComparer.OrdinalIgnoreCase) { "id", "type", "organizationId", "status", "priority" };

    // Shown when the iteration cap forcibly stops the loop, so a cut-off run is never rendered as a
    // clean empty answer.
    private const string CappedMessage =
        "Zaustavljeno nakon maksimalnog broja koraka pre nego sto je zadatak dovrsen. Pokusajte da preformulisete ili suzite zahtev.";

    private readonly IClaudeAgentClient _claude;
    private readonly IMcpGatewayClient _gateway;
    private readonly IAgentTokenService _agentTokens;
    private readonly AgentOptions _options;
    private readonly ILogger<AiChatAgent> _logger;
    private readonly HashSet<string> _readTools;

    public AiChatAgent(
        IClaudeAgentClient claude,
        IMcpGatewayClient gateway,
        IAgentTokenService agentTokens,
        IOptions<AgentOptions> options,
        ILogger<AiChatAgent> logger)
    {
        _claude = claude;
        _gateway = gateway;
        _agentTokens = agentTokens;
        _options = options.Value;
        _logger = logger;
        _readTools = new HashSet<string>(_options.ReadTools ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
    }

    public async Task<AiChatResponseDTO> RunAsync(AiChatRequestDTO request, string oboBearer, CancellationToken cancellationToken)
    {
        var correlationId = string.IsNullOrWhiteSpace(request.CorrelationId)
            ? Guid.NewGuid().ToString()
            : request.CorrelationId!;

        var agentToken = _agentTokens.CreateAgentToken();
        var messages = request.History is { Count: > 0 }
            ? new List<ChatMessageDTO>(request.History)
            : new List<ChatMessageDTO>();

        // Both principals + the correlation id ride on every gateway call in this chain.
        var tools = await _gateway.ListToolsAsync(oboBearer, agentToken, correlationId, cancellationToken);

        // Resolve a pending write proposal FIRST (no Claude call needed to decide it): on approval,
        // execute it through the gateway now; on rejection, tell Claude the user declined. Either
        // way we append a tool_result and fall through so Claude reacts to the outcome.
        if (request.PendingConfirmation is { } pending)
        {
            var proposed = FindToolUse(messages, pending.ToolUseId)
                ?? throw new InvalidOperationException("Predlozena akcija nije pronadjena u istoriji razgovora.");

            if (pending.Approved)
            {
                var writeResult = await _gateway.CallToolAsync(
                    proposed.ToolName!, proposed.Input ?? new JsonObject(),
                    oboBearer, agentToken, correlationId, cancellationToken);
                messages.Add(ToolResult(pending.ToolUseId, writeResult));
            }
            else
            {
                messages.Add(ToolResultText(pending.ToolUseId, "Korisnik je odbio ovu akciju; nije izvrsena.", isError: false));
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Message))
            messages.Add(UserText(request.Message!));

        var iterations = 0;
        string? lastText = null;

        while (iterations < _options.MaxIterations)
        {
            var turn = await _claude.SendAsync(messages, tools, cancellationToken);
            iterations++;
            lastText = turn.AssistantText ?? lastText;

            if (!turn.WantsTools)
            {
                messages.Add(AssistantTurn(turn.AssistantText, toolCall: null));
                return Completed(turn.AssistantText, messages, correlationId, iterations);
            }

            // disable_parallel_tool_use is set on the Claude request, so a turn carries at most one
            // tool call; handle the first.
            var toolCall = turn.ToolCalls[0];
            messages.Add(AssistantTurn(turn.AssistantText, toolCall));

            // FAIL-SAFE gate: only known read tools auto-execute. Anything else — a write, or a tool
            // not on the read allow-list (e.g. one added to the gateway later) — is surfaced as a
            // proposal and waits for the caller to confirm on a follow-up call (human-in-the-loop).
            if (!_readTools.Contains(toolCall.ToolName))
                return PendingConfirmation(toolCall, messages, correlationId, iterations);

            var toolResult = await _gateway.CallToolAsync(
                toolCall.ToolName, toolCall.Input, oboBearer, agentToken, correlationId, cancellationToken);
            messages.Add(ToolResult(toolCall.ToolUseId, toolResult));
        }

        // Iteration cap reached — stop forcibly. Never present a cut-off run as a clean empty answer:
        // fall back to an explicit message when the model produced no text on its final turn.
        _logger.LogWarning("AiChat iteration cap ({Cap}) reached; forcing stop. CorrelationId={CorrelationId}",
            _options.MaxIterations, correlationId);
        return Completed(string.IsNullOrWhiteSpace(lastText) ? CappedMessage : lastText, messages, correlationId, iterations);
    }

    // ── history builders ─────────────────────────────────────────────────────────
    private static ChatMessageDTO UserText(string text) => new()
    {
        Role = "user",
        Content = { new ChatContentDTO { Type = "text", Text = text } },
    };

    private static ChatMessageDTO AssistantTurn(string? text, ClaudeToolCall? toolCall)
    {
        var msg = new ChatMessageDTO { Role = "assistant" };
        if (!string.IsNullOrEmpty(text))
            msg.Content.Add(new ChatContentDTO { Type = "text", Text = text });
        if (toolCall is not null)
            msg.Content.Add(new ChatContentDTO
            {
                Type = "tool_use",
                ToolUseId = toolCall.ToolUseId,
                ToolName = toolCall.ToolName,
                Input = toolCall.Input,
            });
        return msg;
    }

    private static ChatMessageDTO ToolResult(string toolUseId, ToolCallResult result) =>
        ToolResultText(toolUseId, result.Content, result.IsError);

    private static ChatMessageDTO ToolResultText(string toolUseId, string content, bool isError) => new()
    {
        Role = "user",
        Content =
        {
            new ChatContentDTO
            {
                Type = "tool_result",
                ToolUseId = toolUseId,
                ResultContent = content,
                IsError = isError,
            },
        },
    };

    /// <summary>Finds the tool_use block with the given id in the (client-echoed) history — the
    /// write the caller is now confirming. Searches newest-first.</summary>
    private static ChatContentDTO? FindToolUse(List<ChatMessageDTO> messages, string toolUseId)
    {
        for (var i = messages.Count - 1; i >= 0; i--)
        {
            var block = messages[i].Content
                .FirstOrDefault(c => c.Type == "tool_use" && c.ToolUseId == toolUseId);
            if (block is not null)
                return block;
        }
        return null;
    }

    private static AiChatResponseDTO Completed(string? text, List<ChatMessageDTO> messages, string correlationId, int iterations) => new()
    {
        Status = AiChatStatus.Completed,
        AssistantText = text,
        History = messages,
        CorrelationId = correlationId,
        Iterations = iterations,
    };

    private static AiChatResponseDTO PendingConfirmation(ClaudeToolCall toolCall, List<ChatMessageDTO> messages, string correlationId, int iterations) => new()
    {
        Status = AiChatStatus.PendingConfirmation,
        Proposal = new ToolProposalDTO
        {
            ToolUseId = toolCall.ToolUseId,
            ToolName = toolCall.ToolName,
            ArgsSummary = BuildArgsSummary(toolCall.Input),
        },
        History = messages,
        CorrelationId = correlationId,
        Iterations = iterations,
    };

    /// <summary>Human-readable, scrubbed summary of a proposed write's args — safe keys verbatim,
    /// everything else masked so a password / name / free-text never reaches the caller here.</summary>
    private static string BuildArgsSummary(JsonNode? input)
    {
        if (input is not JsonObject obj) return string.Empty;
        var parts = new List<string>();
        foreach (var kvp in obj)
        {
            var value = SafeArgKeys.Contains(kvp.Key) ? (kvp.Value?.ToString() ?? "null") : "<hidden>";
            parts.Add($"{kvp.Key}={value}");
        }
        return string.Join(", ", parts);
    }
}
