using System.Text.Json;
using McpGateway.Audit;
using Xunit;

namespace McpGatewayTests;

/// <summary>The audit args summary must never echo free-text (potential PII); only whitelisted keys log literally.</summary>
public class ArgsSummaryTests
{
    private static IDictionary<string, JsonElement> Args(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;

    [Fact]
    public void Null_or_empty_args_produce_null()
    {
        Assert.Null(ArgsSummary.Build(null));
        Assert.Null(ArgsSummary.Build(Args("{}")));
    }

    [Fact]
    public void Whitelisted_keys_are_literal_free_text_is_length_only()
    {
        var summary = ArgsSummary.Build(Args("""{ "type": "problem", "keyword": "harassment" }"""));

        Assert.Contains("type=problem", summary);        // identifier/flag — safe literal
        Assert.Contains("keyword=<len:10>", summary);    // free text — scrubbed to length
        Assert.DoesNotContain("harassment", summary);    // never the literal value
    }

    [Fact]
    public void Id_is_logged_literally()
    {
        var id = Guid.NewGuid();
        Assert.Contains($"id={id}", ArgsSummary.Build(Args($$"""{ "id": "{{id}}" }""")));
    }

    [Fact]
    public void Unknown_keys_are_scrubbed_to_length()
    {
        var summary = ArgsSummary.Build(Args("""{ "description": "secret personal detail" }"""));
        Assert.DoesNotContain("secret", summary);
        Assert.Contains("description=<len:", summary);
    }
}
