using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Config;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Hosts;
using DnsmasqWebUI.Models.Config;
using DnsmasqWebUI.Models.Hosts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Tests.Services.Hosts;

public sealed class HostsCacheSourceResolutionTests
{
    [Fact]
    public async Task Snapshot_keeps_system_source_inactive_but_keeps_same_path_addn_hosts_active_when_no_hosts_is_set()
    {
        var fixture = CreateFixture(noHosts: true, includeSystemAsAddnHosts: true);
        try
        {
            var snapshot = await fixture.HostsCache.GetSnapshotAsync();
            var rows = await fixture.HostsCache.GetUnifiedRowsAsync();

            var systemSource = Assert.Single(snapshot.Sources, source => source.SourceKind == HostsRowSourceKind.SystemHosts);
            Assert.False(systemSource.IsActive);
            Assert.Contains("no-hosts", systemSource.InactiveReason!, StringComparison.OrdinalIgnoreCase);

            var systemAsAddn = Assert.Single(snapshot.Sources, source =>
                source.SourceKind == HostsRowSourceKind.AddnHosts &&
                string.Equals(source.Path, fixture.SystemHostsPath, StringComparison.Ordinal));
            Assert.True(systemAsAddn.IsActive);

            var systemRows = rows.Where(row => string.Equals(row.SourcePath, fixture.SystemHostsPath, StringComparison.Ordinal)).ToList();
            Assert.Contains(systemRows, row => row.SourceKind == HostsRowSourceKind.SystemHosts && !row.IsActive);
            Assert.Contains(systemRows, row => row.SourceKind == HostsRowSourceKind.AddnHosts && row.IsActive);
        }
        finally
        {
            fixture.Dispose();
        }
    }

    [Fact]
    public async Task Snapshot_expands_addn_hosts_and_hostsdir_directories_in_alphabetical_order_and_reports_missing_files()
    {
        var fixture = CreateFixture(noHosts: false, includeSystemAsAddnHosts: false);
        try
        {
            var snapshot = await fixture.HostsCache.GetSnapshotAsync();

            var addnMembers = snapshot.Sources
                .Where(source => source.SourceKind == HostsRowSourceKind.AddnHosts &&
                    string.Equals(source.ConfiguredPath, fixture.AddnDirectory, StringComparison.Ordinal) &&
                    !source.IsDirectory)
                .Select(source => Path.GetFileName(source.Path))
                .ToList();
            Assert.Equal(new[] { "a.hosts", "b.hosts" }, addnMembers);

            var hostsdirMembers = snapshot.Sources
                .Where(source => source.SourceKind == HostsRowSourceKind.Hostsdir && !source.IsDirectory)
                .Select(source => Path.GetFileName(source.Path))
                .ToList();
            Assert.Equal(new[] { "first.hosts", "second.hosts" }, hostsdirMembers);

            var missing = Assert.Single(snapshot.Sources, source => string.Equals(source.Path, fixture.MissingPath, StringComparison.Ordinal));
            Assert.Equal("File not found.", missing.Diagnostic);
            Assert.Contains(snapshot.ReadOnlyFiles, file =>
                string.Equals(file.Path, fixture.MissingPath, StringComparison.Ordinal) &&
                file.Entries.Count == 0 &&
                file.Diagnostic == "File not found.");
        }
        finally
        {
            fixture.Dispose();
        }
    }

    [Fact]
    public async Task Directory_member_changes_refresh_snapshot_even_if_a_file_system_event_is_missed()
    {
        var fixture = CreateFixture(noHosts: false, includeSystemAsAddnHosts: false);
        try
        {
            var before = await fixture.HostsCache.GetSnapshotAsync();
            Assert.DoesNotContain(before.Sources, source => Path.GetFileName(source.Path) == "late.hosts");

            File.WriteAllText(Path.Combine(fixture.AddnDirectory, "late.hosts"), "192.0.2.90 late\n");

            // HostsCache compares the resolved source shape on every read, so the new member is visible
            // without relying on watcher delivery timing.
            var after = await fixture.HostsCache.GetSnapshotAsync();
            Assert.Contains(after.Sources, source => Path.GetFileName(source.Path) == "late.hosts");
            Assert.Contains(after.ReadOnlyFiles, file => Path.GetFileName(file.Path) == "late.hosts");
        }
        finally
        {
            fixture.Dispose();
        }
    }

    [Fact]
    public async Task External_source_content_change_refreshes_cached_entries_without_waiting_for_the_stale_timeout()
    {
        var fixture = CreateFixture(noHosts: false, includeSystemAsAddnHosts: false);
        try
        {
            var first = await fixture.HostsCache.GetSnapshotAsync();
            Assert.Contains(first.ReadOnlyFiles.SelectMany(file => file.Entries), entry => entry.Names.Contains("alpha"));

            File.WriteAllText(Path.Combine(fixture.AddnDirectory, "a.hosts"), "192.0.2.21 replacement-name\n");

            var refreshed = await fixture.HostsCache.GetSnapshotAsync();
            var entries = refreshed.ReadOnlyFiles.SelectMany(file => file.Entries).ToList();
            Assert.DoesNotContain(entries, entry => entry.Names.Contains("alpha"));
            Assert.Contains(entries, entry => entry.Names.Contains("replacement-name"));
        }
        finally
        {
            fixture.Dispose();
        }
    }

    [Fact]
    public async Task Legacy_unified_row_arguments_cannot_override_authoritative_source_state()
    {
        var fixture = CreateFixture(noHosts: true, includeSystemAsAddnHosts: false);
        try
        {
            var rows = await fixture.HostsCache.GetUnifiedRowsAsync(
                expandHosts: false,
                noHosts: false,
                managedHostsPath: "/client-controlled/not-managed.hosts");

            var systemRow = Assert.Single(rows, row => row.SourceKind == HostsRowSourceKind.SystemHosts);
            Assert.False(systemRow.IsActive);
            Assert.Equal(fixture.SystemHostsPath, systemRow.SourcePath);

            var managedRow = Assert.Single(rows, row => row.SourceKind == HostsRowSourceKind.Managed);
            Assert.NotEqual("/client-controlled/not-managed.hosts", managedRow.SourcePath);
        }
        finally
        {
            fixture.Dispose();
        }
    }

    private static HostsFixture CreateFixture(bool noHosts, bool includeSystemAsAddnHosts)
    {
        var directory = Path.Combine(Path.GetTempPath(), "dnsmasq-hosts-cache-" + Guid.NewGuid().ToString("N"));
        var addnDirectory = Path.Combine(directory, "addn");
        var hostsdirDirectory = Path.Combine(directory, "hostsdir");
        var systemHostsPath = Path.Combine(directory, "system.hosts");
        var managedConfigPath = Path.Combine(directory, "zz-managed.conf");
        var managedHostsPath = Path.Combine(directory, "zz-managed.hosts");
        var missingPath = Path.Combine(directory, "missing.hosts");
        Directory.CreateDirectory(directory);
        Directory.CreateDirectory(addnDirectory);
        Directory.CreateDirectory(hostsdirDirectory);

        File.WriteAllText(systemHostsPath, "192.0.2.10 system-name\n");
        File.WriteAllText(Path.Combine(addnDirectory, "b.hosts"), "192.0.2.20 beta\n");
        File.WriteAllText(Path.Combine(addnDirectory, "a.hosts"), "192.0.2.21 alpha\n");
        File.WriteAllText(Path.Combine(hostsdirDirectory, "second.hosts"), "192.0.2.30 second\n");
        File.WriteAllText(Path.Combine(hostsdirDirectory, "first.hosts"), "192.0.2.31 first\n");
        File.WriteAllText(managedHostsPath, "192.0.2.40 managed\n");
        File.WriteAllText(managedConfigPath, $"addn-hosts={managedHostsPath}\n");

        var configLines = new List<string>();
        if (noHosts)
            configLines.Add("no-hosts");
        if (includeSystemAsAddnHosts)
            configLines.Add($"addn-hosts={systemHostsPath}");
        configLines.Add($"addn-hosts={addnDirectory}");
        configLines.Add($"addn-hosts={missingPath}");
        configLines.Add($"hostsdir={hostsdirDirectory}");
        var mainConfigPath = Path.Combine(directory, "dnsmasq.conf");
        File.WriteAllLines(mainConfigPath, configLines);

        var options = Options.Create(new DnsmasqOptions
        {
            MainConfigPath = mainConfigPath,
            ManagedFileName = Path.GetFileName(managedConfigPath),
            ManagedHostsFileName = Path.GetFileName(managedHostsPath),
            SystemHostsPath = systemHostsPath
        });
        var configCache = new ConfigSetCache(options, NullLogger<ConfigSetCache>.Instance);
        var hostsCache = new HostsCache(
            configCache,
            new HostSourceResolver(options),
            NullLogger<HostsCache>.Instance);
        return new HostsFixture(
            directory,
            addnDirectory,
            hostsdirDirectory,
            systemHostsPath,
            missingPath,
            configCache,
            hostsCache);
    }

    private sealed class HostsFixture : IDisposable
    {
        public HostsFixture(
            string directory,
            string addnDirectory,
            string hostsdirDirectory,
            string systemHostsPath,
            string missingPath,
            ConfigSetCache configCache,
            HostsCache hostsCache)
        {
            Directory = directory;
            AddnDirectory = addnDirectory;
            HostsdirDirectory = hostsdirDirectory;
            SystemHostsPath = systemHostsPath;
            MissingPath = missingPath;
            ConfigCache = configCache;
            HostsCache = hostsCache;
        }

        public string Directory { get; }
        public string AddnDirectory { get; }
        public string HostsdirDirectory { get; }
        public string SystemHostsPath { get; }
        public string MissingPath { get; }
        public ConfigSetCache ConfigCache { get; }
        public HostsCache HostsCache { get; }

        public void Dispose()
        {
            HostsCache.Dispose();
            ConfigCache.Dispose();
            if (System.IO.Directory.Exists(Directory))
                System.IO.Directory.Delete(Directory, recursive: true);
        }
    }
}
