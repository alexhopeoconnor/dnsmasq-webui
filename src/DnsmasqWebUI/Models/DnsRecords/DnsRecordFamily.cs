namespace DnsmasqWebUI.Models.DnsRecords;

/// <summary>Logical DNS record family for UI grouping and filtering.</summary>
public enum DnsRecordFamily
{
    Cname,
    HostRecord,
    Txt,
    Ptr,
    Mx,
    Srv,
    Naptr,
    Caa,
    DnsRr,
    DynamicHost,
    InterfaceName,
    SynthDomain,
    AuthZone,
    AuthSoa,
    AuthSecServers,
    AuthPeer,
}

/// <summary>Toolbar chip selection (maps to <see cref="DnsRecordsQueryState"/>).</summary>
public enum DnsRecordsUiFamily
{
    All,
    Cname,
    HostRecord,
    Txt,
    Ptr,
    Mx,
    Srv,
    Advanced,
}

public static class DnsRecordFamilyExtensions
{
    public static bool IsAdvancedFamily(this DnsRecordFamily f) => f is
        DnsRecordFamily.Naptr or DnsRecordFamily.Caa or DnsRecordFamily.DnsRr
        or DnsRecordFamily.DynamicHost or DnsRecordFamily.InterfaceName
        or DnsRecordFamily.SynthDomain or DnsRecordFamily.AuthZone or DnsRecordFamily.AuthSoa
        or DnsRecordFamily.AuthSecServers or DnsRecordFamily.AuthPeer;
}
