using DnsmasqWebUI.Models.DnsRecords;

namespace DnsmasqWebUI.Components.DnsRecords.Editors;

/// <summary>
/// Blazor calls <see cref="Microsoft.AspNetCore.Components.ComponentBase.OnParametersSet"/> on every parent render,
/// not only when parameters change. Editors must not overwrite bound fields unless the edited row (or add-new session)
/// actually changed.
/// </summary>
internal static class DnsRecordEditorSyncGuard
{
    /// <param name="lastBoundKey">Previous session key; updated when reseed runs.</param>
    /// <param name="existing">Row being edited, or null for add-new.</param>
    /// <param name="addModeKeyWhenNew">
    /// When <paramref name="existing"/> is null, distinguishes add sessions (e.g. <c>add:naptr-record</c> vs <c>add:caa-record</c>).
    /// Omit for editors that only ever add one directive type per component instance.
    /// </param>
    public static bool ShouldReseedFromExisting(ref string? lastBoundKey, DnsRecordRow? existing, string? addModeKeyWhenNew = null)
    {
        var key = existing?.Id ?? addModeKeyWhenNew ?? "";
        if (string.Equals(lastBoundKey, key, StringComparison.Ordinal))
            return false;
        lastBoundKey = key;
        return true;
    }
}
