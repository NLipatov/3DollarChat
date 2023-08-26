namespace Limp.Client.Services.UserAgentService.Models;

public record UserAgentInformation
{
    public string UserAgentDescription { get; set; } = "N/A";
    public Guid UserAgentId { get; set; }
}