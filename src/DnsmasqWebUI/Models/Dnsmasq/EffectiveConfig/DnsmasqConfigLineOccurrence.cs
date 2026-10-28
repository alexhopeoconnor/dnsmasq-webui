namespace DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

/// <summary>
/// One directive line as dnsmasq encounters it while expanding <c>conf-file</c> and
/// <c>conf-dir</c> directives in place. This is deliberately an occurrence rather
/// than a file: the main file can continue after an included file has been read.
/// </summary>
/// <param name="FilePath">Absolute path of the source config file.</param>
/// <param name="LineNumber">One-based line number in <paramref name="FilePath"/>.</param>
/// <param name="Line">The original directive line.</param>
/// <param name="Source">How dnsmasq reached the source file.</param>
public sealed record DnsmasqConfigLineOccurrence(
    string FilePath,
    int LineNumber,
    string Line,
    DnsmasqConfFileSource Source);
