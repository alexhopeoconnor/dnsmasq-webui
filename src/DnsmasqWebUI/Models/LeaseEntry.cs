namespace DnsmasqWebUI.Models;

public class LeaseEntry
{
    public long Epoch { get; set; }
    public string Mac { get; set; } = "";
    public string Address { get; set; } = "";
    public string Name { get; set; } = "";
    public string ClientId { get; set; } = "";
    public DateTime Timestamp => DateTimeOffset.FromUnixTimeSeconds(Epoch).DateTime;
}
