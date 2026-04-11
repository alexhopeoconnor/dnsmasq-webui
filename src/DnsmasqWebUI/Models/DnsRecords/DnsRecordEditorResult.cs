namespace DnsmasqWebUI.Models.DnsRecords;

/// <summary>Result from a structured record editor (add or update).</summary>
public sealed record DnsRecordEditorResult(DnsRecordRow Row, bool IsNew);
