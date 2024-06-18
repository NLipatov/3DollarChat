using EthachatShared.Models.Authentication.Enums;

namespace EthachatShared.Models.Authentication.Models;

public record AccessRefreshEventLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserAgent { get; set; }
    public Guid UserAgentId { get; set; }
    public DateTime DateTime { get; set; } = DateTime.UtcNow;
    public JwtIssueReason IssueReason { get; set; }
}