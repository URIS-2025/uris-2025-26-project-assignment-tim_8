using System.Text.Json;
using AiAssistantService.Agent;
using AiAssistantService.Clients;
using AiAssistantService.Models;
using Anthropic.Models.Messages;
using Xunit;

namespace AiAssistantServiceTests;

/// <summary>
/// Locks the request shape <see cref="ClaudeClient"/> sends to the Anthropic Messages API.
///
/// The load-bearing assertion is <see cref="Thinking_is_explicitly_disabled"/>. On
/// <c>claude-sonnet-5</c>, OMITTING the <c>thinking</c> parameter runs ADAPTIVE thinking — a silent
/// default change from Sonnet 4.6, where omitting it meant thinking was off. With thinking on,
/// Claude's assistant turn contains a thinking block before its <c>tool_use</c> block, and the API
/// requires that block to be echoed back unmodified (its signature is validated) on the next
/// request. This agent cannot do that: <see cref="ChatContentDTO"/> has no "thinking" type, so the
/// block cannot round-trip. The result was an intermittent 400 on the turn immediately after any
/// tool call — surfacing to the user only as the fixed "asistent trenutno ne može" message.
///
/// Disabling thinking explicitly restores the Sonnet-4.6 behaviour this agent loop was written
/// against. If a future change wants thinking ON, it must first give ChatContentDTO a "thinking"
/// type that preserves Signature and carry it through AiChatAgent.AssistantTurn — at which point
/// this test should be updated deliberately, not deleted.
/// </summary>
public class ClaudeClientParamsTests
{
    private static readonly AgentOptions Options = new()
    {
        Model = "claude-sonnet-5",
        MaxTokens = 4096,
        SystemPrompt = "test-system",
    };

    private static IReadOnlyList<ChatMessageDTO> OneUserTurn() =>
    [
        new ChatMessageDTO
        {
            Role = "user",
            Content = [new ChatContentDTO { Type = "text", Text = "zdravo" }],
        },
    ];

    private static IReadOnlyList<ToolDefinition> OneTool() =>
    [
        new ToolDefinition(
            "list_boxes",
            "Lists boxes.",
            JsonDocument.Parse("""{"type":"object","properties":{},"required":[]}""").RootElement),
    ];

    [Fact]
    public void Thinking_is_explicitly_disabled()
    {
        var parameters = ClaudeClient.BuildParams(OneUserTurn(), OneTool(), Options);

        Assert.NotNull(parameters.Thinking);
        Assert.True(
            parameters.Thinking!.TryPickDisabled(out ThinkingConfigDisabled? disabled),
            "Thinking must be explicitly disabled: on claude-sonnet-5 an omitted `thinking` " +
            "parameter runs adaptive thinking, and this agent cannot round-trip thinking blocks.");
        Assert.NotNull(disabled);
    }

    [Fact]
    public void Parallel_tool_use_stays_disabled_so_a_turn_carries_at_most_one_tool_call()
    {
        var parameters = ClaudeClient.BuildParams(OneUserTurn(), OneTool(), Options);

        Assert.NotNull(parameters.ToolChoice);
        Assert.True(parameters.ToolChoice!.TryPickAuto(out ToolChoiceAuto? auto));
        Assert.True(auto!.DisableParallelToolUse);
    }

    [Fact]
    public void Model_and_max_tokens_come_from_options()
    {
        var parameters = ClaudeClient.BuildParams(OneUserTurn(), OneTool(), Options);

        // Model is an SDK union type; its ToString() is the JSON form, i.e. quote-wrapped.
        Assert.Equal("claude-sonnet-5", parameters.Model.ToString().Trim('"'));
        Assert.Equal(4096, parameters.MaxTokens);
    }
}
