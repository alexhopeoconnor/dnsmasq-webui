namespace DnsmasqWebUI.Models.Logs;

/// <summary>
/// Payload sent via SignalR for log updates. Content is chunked by complete lines, max ~28KB per message.
/// </summary>
public sealed record LogsUpdatePayload(string Mode, string Content)
{
    /// <summary>"append" = add to existing; "replace" = clear then set.</summary>
    public string Mode { get; } = Mode;

    /// <summary>One or more complete lines. Never split mid-line.</summary>
    public string Content { get; } = Content;
}
