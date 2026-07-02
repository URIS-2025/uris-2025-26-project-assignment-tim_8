using System.Text.Json;

namespace McpGateway.Audit;

/// <summary>
/// Builds a SCRUBBED, PII-safe one-line summary of a tool call's arguments for the audit record.
/// Deny-by-default: only a small whitelist of identifier/flag keys is logged literally; every other
/// argument (e.g. a search keyword, which may echo report content or personal data) is reduced to a
/// length only. The audit trail must never itself become a data-leak.
/// </summary>
public static class ArgsSummary
{
    private static readonly HashSet<string> SafeLiteral = new(StringComparer.OrdinalIgnoreCase)
    {
        "type", "id", "organizationId", "status", "priority",
    };

    private const int MaxLiteral = 64;

    public static string? Build(IDictionary<string, JsonElement>? args)
    {
        if (args is null || args.Count == 0) return null;

        var parts = args
            .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kv => SafeLiteral.Contains(kv.Key)
                ? $"{kv.Key}={Literal(kv.Value)}"
                : $"{kv.Key}=<len:{Text(kv.Value).Length}>");

        return string.Join("; ", parts);
    }

    private static string Literal(JsonElement value)
    {
        var s = Text(value);
        return s.Length > MaxLiteral ? s[..MaxLiteral] : s; // cap even "safe" values
    }

    private static string Text(JsonElement value) =>
        value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : value.ToString();
}
