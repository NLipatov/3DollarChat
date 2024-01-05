namespace Ethachat.Client.Services.UserAgent.Models;

public record UserAgentInformation
{
    public string? UserAgentDescription { get; init; }
    public Guid UserAgentId { get; init; }
}