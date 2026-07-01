using System.ComponentModel;
using ModelContextProtocol.Server;

namespace McpGateway.Tools;

/// <summary>
/// Write tools (state-changing actions). Faza A: STUBS. In later phases these call the existing
/// REST endpoints (inheriting their validation + audit + fail-closed rules) and are gated behind
/// human-in-the-loop confirmation. Tool names match the policy keys in appsettings.
/// </summary>
[McpServerToolType]
public static class WriteTools
{
    private const string Todo = "TODO (Faza B): tool not yet implemented";

    [McpServerTool(Name = "set_box_status"), Description("Activate or deactivate a box.")]
    public static string SetBoxStatus() => Todo;

    [McpServerTool(Name = "update_submission_status"), Description("Change the status of a problem or suggestion.")]
    public static string UpdateSubmissionStatus() => Todo;

    [McpServerTool(Name = "add_comment"), Description("Post a comment (reply) on a problem or suggestion.")]
    public static string AddComment() => Todo;

    [McpServerTool(Name = "set_box_password"), Description("Set or clear a box password.")]
    public static string SetBoxPassword() => Todo;

    [McpServerTool(Name = "create_box"), Description("Create a new problem or suggestion box.")]
    public static string CreateBox() => Todo;
}
