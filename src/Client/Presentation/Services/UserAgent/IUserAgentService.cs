using Ethachat.Client.Services.UserAgent.Models;

namespace Ethachat.Client.Services.UserAgent;

public interface IUserAgentService
{
    public Task<UserAgentInformation> GetUserAgentInformation();
}