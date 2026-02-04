namespace DnsmasqWebUI.Models.EffectiveConfig;

/// <summary>
/// Where an effective config value came from (which file and line). Used so the UI can show exactly which file
/// set the value and why it is readonly or editable.
/// </summary>
/// <param name="FilePath">Absolute path of the config file that set this value. Use for "edit this file" or tooltips.</param>
/// <param name="FileName">Filename only (e.g. dnsmasq.conf, 01-default.conf) for compact display.</param>
/// <param name="IsManaged">True when this file is the app's managed config file (editable from UI).</param>
/// <param name="LineNumber">1-based line number in the file where the value was set; null if unknown.</param>
/// <remarks>
/// When <see cref="IsManaged"/> is true: the user can change or remove the value from the UI (we read/write the managed file).
/// When false (<see cref="IsReadOnly"/> is true): the value is from main or an included conf file; the user must edit that file to change it.
/// For single-value or multi-value options: show "from FileName (readonly)" and e.g. "Edit FilePath to change."
/// For flags: if the flag is set in a non-managed file, the user cannot unset it from the UI (they must remove the line in that file).
/// </remarks>
public record ConfigValueSource(string FilePath, string FileName, bool IsManaged, int? LineNumber = null)
{
    /// <summary>True when the value is from a non-managed file: user cannot change it from the UI; for flags, cannot unset without editing that file.</summary>
    public bool IsReadOnly => !IsManaged;

    /// <summary>Returns a short tooltip for readonly values (which file, optional line, and how to change). Null when <see cref="IsManaged"/> (editable).</summary>
    public string? GetReadOnlyTooltip() =>
        IsReadOnly
            ? LineNumber.HasValue
                ? $"From {FileName} line {LineNumber} (readonly). Edit {FilePath} to change."
                : $"From {FileName} (readonly). Edit {FilePath} to change."
            : null;
}

/// <summary>Value plus its config file source. Used for multi-value options (server/local, address, etc.) so JSON serializes as "value" and "source".</summary>
public record ValueWithSource(string Value, ConfigValueSource? Source);

/// <summary>Path plus its config file source. Used for addn-hosts so JSON serializes as "path" and "source".</summary>
public record PathWithSource(string Path, ConfigValueSource? Source);
