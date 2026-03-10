using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Helpers.Config;

/// <summary>
/// Shared validation kinds for effective-config options.
/// Describes the generic value shape; option-specific rules live in the semantic validator or handlers.
/// </summary>
public enum OptionValidationKind
{
    Flag,
    Int,
    String,
    PathFile,
    PathDirectory,
    PathFileOrDirectory,
    IpAddress,
    HostOrIp,
    KeyOnlyOrValue,
    InversePair,
    Complex
}

/// <summary>
/// Whether a path value must exist (file/dir) or only its parent, for validation.
/// </summary>
public enum PathExistencePolicy
{
    None,
    MustExist,
    ParentMustExist
}

/// <summary>
/// Reusable validation metadata for an option: validation kind, path policy, severity, and empty handling.
/// </summary>
public sealed record OptionValidationSemantics
{
    public OptionValidationKind Kind { get; }
    public FieldIssueSeverity Severity { get; }
    public bool AllowEmpty { get; }
    public PathExistencePolicy? PathPolicy { get; }

    public OptionValidationSemantics(
        OptionValidationKind kind,
        FieldIssueSeverity severity = FieldIssueSeverity.Error,
        bool allowEmpty = true,
        PathExistencePolicy? pathPolicy = null)
    {
        if (pathPolicy is not null && kind is not (
            OptionValidationKind.PathFile or
            OptionValidationKind.PathDirectory or
            OptionValidationKind.PathFileOrDirectory))
        {
            throw new ArgumentException("PathPolicy can only be used with path validation kinds.", nameof(pathPolicy));
        }

        Kind = kind;
        Severity = severity;
        AllowEmpty = allowEmpty;
        PathPolicy = pathPolicy;
    }
}
