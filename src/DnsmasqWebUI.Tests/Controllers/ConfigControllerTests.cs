using DnsmasqWebUI.Controllers;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Config.Abstractions;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace DnsmasqWebUI.Tests.Controllers;

public sealed class ConfigControllerTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RawManagedConfigEndpoints_AreExplicitlyRetired(bool isWrite)
    {
        var controller = new ConfigController(
            new UnusedConfigSetService(),
            NullLogger<ConfigController>.Instance);

        var result = isWrite ? controller.PutManaged() : controller.GetManaged();

        var response = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status410Gone, response.StatusCode);

        var problem = Assert.IsType<ProblemDetails>(response.Value);
        Assert.Equal("Raw managed-config endpoint retired", problem.Title);
        Assert.Contains("Effective Config", problem.Detail, StringComparison.Ordinal);
    }

    private sealed class UnusedConfigSetService : IDnsmasqConfigSetService
    {
        public Task<DnsmasqConfigSet> GetConfigSetAsync(CancellationToken ct = default) =>
            throw new NotSupportedException("The retired endpoint must not read configuration.");

        public string? GetLeasesPath() => throw new NotSupportedException();

        public EffectiveDnsmasqConfig GetEffectiveConfig() => throw new NotSupportedException();

        public (EffectiveDnsmasqConfig Config, EffectiveConfigSources Sources) GetEffectiveConfigWithSources() =>
            throw new NotSupportedException();

        public IReadOnlyList<string> GetAddnHostsPaths() => throw new NotSupportedException();

        public (string? Start, string? End) GetDhcpRange() => throw new NotSupportedException();
    }
}
