namespace DnsmasqWebUI.Infrastructure.Services.Common.Process;

/// <summary>Which stream a line came from when reading process output asynchronously.</summary>
public enum ProcessOutputStream
{
    StdOut,
    StdErr,
}

/// <summary>Single line of process output (stdout or stderr) with timestamp. Used by <see cref="Abstractions.IProcessHandle.ReadOutputAsync"/>.</summary>
public record ProcessOutputLine(
    ProcessOutputStream Stream,
    string Line,
    DateTime TimestampUtc);
