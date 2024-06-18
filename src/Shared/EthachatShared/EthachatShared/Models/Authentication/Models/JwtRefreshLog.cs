namespace EthachatShared.Models.Authentication.Models;

public record JwtRefreshLog
{
    public string UserAgentDescription { get; set; } = string.Empty;
    public string UserAgentId { get; set; } = string.Empty;
    public DateTime DateTime { get; set; }
}