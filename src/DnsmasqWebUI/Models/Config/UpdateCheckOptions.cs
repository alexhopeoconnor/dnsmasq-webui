namespace DnsmasqWebUI.Models.Config;

/// <summary>
/// Options for the periodic update check (GitHub latest release). Bound from "UpdateCheck" config section.
/// </summary>
public class UpdateCheckOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "UpdateCheck";

    /// <summary>Interval in minutes between checks. Default 1440 (24 hours). Set to 0 to disable periodic checks.</summary>
    public int IntervalMinutes { get; set; } = 1440;
}
