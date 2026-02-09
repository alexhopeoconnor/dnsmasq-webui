using Microsoft.Extensions.Logging;

namespace DnsmasqWebUI.Infrastructure.Logging;

/// <summary>Structured event IDs for application logging. Enables filtering and alerting by event type.</summary>
internal static class LogEvents
{
    // Config (1xxx)
    public static readonly EventId ConfigGetSetFailed = new(1001, nameof(ConfigGetSetFailed));
    public static readonly EventId ConfigGetManagedFailed = new(1002, nameof(ConfigGetManagedFailed));
    public static readonly EventId ConfigPutManagedFailed = new(1003, nameof(ConfigPutManagedFailed));
    public static readonly EventId ConfigPutManagedSuccess = new(1004, nameof(ConfigPutManagedSuccess));
    public static readonly EventId ConfigNoManagedFilePath = new(1005, nameof(ConfigNoManagedFilePath));
    public static readonly EventId ConfigWroteManagedFile = new(1006, nameof(ConfigWroteManagedFile));

    // Hosts (2xxx)
    public static readonly EventId HostsGetFailed = new(2001, nameof(HostsGetFailed));
    public static readonly EventId HostsGetReadOnlyFailed = new(2002, nameof(HostsGetReadOnlyFailed));
    public static readonly EventId HostsPutFailed = new(2003, nameof(HostsPutFailed));
    public static readonly EventId HostsPutValidationFailed = new(2004, nameof(HostsPutValidationFailed));
    public static readonly EventId HostsPutSuccess = new(2005, nameof(HostsPutSuccess));
    public static readonly EventId HostsWroteManagedFile = new(2006, nameof(HostsWroteManagedFile));

    // Dhcp (3xxx)
    public static readonly EventId DhcpGetHostsFailed = new(3001, nameof(DhcpGetHostsFailed));
    public static readonly EventId DhcpPutHostsFailed = new(3002, nameof(DhcpPutHostsFailed));
    public static readonly EventId DhcpPutHostsValidationFailed = new(3003, nameof(DhcpPutHostsValidationFailed));
    public static readonly EventId DhcpPutHostsSuccess = new(3004, nameof(DhcpPutHostsSuccess));

    // Reload (4xxx)
    public static readonly EventId ReloadRequestFailed = new(4001, nameof(ReloadRequestFailed));
    public static readonly EventId ReloadRequestSuccess = new(4002, nameof(ReloadRequestSuccess));
    public static readonly EventId ReloadCommandNotConfigured = new(4003, nameof(ReloadCommandNotConfigured));
    public static readonly EventId ReloadRejectedConcurrent = new(4004, nameof(ReloadRejectedConcurrent));
    public static readonly EventId ReloadNonZeroExit = new(4005, nameof(ReloadNonZeroExit));
    public static readonly EventId ReloadSucceeded = new(4006, nameof(ReloadSucceeded));
    public static readonly EventId ReloadFailed = new(4007, nameof(ReloadFailed));

    // Status (5xxx)
    public static readonly EventId StatusGetFailed = new(5001, nameof(StatusGetFailed));
    public static readonly EventId StatusLogsDownloadFailed = new(5002, nameof(StatusLogsDownloadFailed));

    // Leases (6xxx)
    public static readonly EventId LeasesGetFailed = new(6001, nameof(LeasesGetFailed));

    // Logging (7xxx)
    public static readonly EventId LogLevelChanged = new(7001, nameof(LogLevelChanged));
    public static readonly EventId LogLevelSetBadRequest = new(7002, nameof(LogLevelSetBadRequest));
    public static readonly EventId LogLevelSetFailed = new(7003, nameof(LogLevelSetFailed));
    public static readonly EventId FiltersChanged = new(7004, nameof(FiltersChanged));
    public static readonly EventId FiltersSetFailed = new(7005, nameof(FiltersSetFailed));

    // LogsService (8010)
    public static readonly EventId DnsmasqLogsPushFailed = new(8010, nameof(DnsmasqLogsPushFailed));

    // ProcessRunner (8xxx)
    public static readonly EventId CommandFailed = new(8001, nameof(CommandFailed));
    public static readonly EventId CommandTimeout = new(8002, nameof(CommandTimeout));
    public static readonly EventId CommandStarted = new(8003, nameof(CommandStarted));
    public static readonly EventId CommandCompleted = new(8004, nameof(CommandCompleted));

    // LeasesFileService (9xxx)
    public static readonly EventId LeasesReadSuccess = new(9001, nameof(LeasesReadSuccess));

    // Application lifecycle (9xxx)
    public static readonly EventId ApplicationStarted = new(9101, nameof(ApplicationStarted));
    public static readonly EventId ApplicationStopping = new(9102, nameof(ApplicationStopping));
    public static readonly EventId ManagedConfigConfFileLineSet = new(9110, nameof(ManagedConfigConfFileLineSet));
    public static readonly EventId ManagedConfigCreatedAtStartup = new(9111, nameof(ManagedConfigCreatedAtStartup));
}
