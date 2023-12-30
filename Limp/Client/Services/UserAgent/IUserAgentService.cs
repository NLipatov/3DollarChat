using Ethachat.Client.Services.UserAgent.Models;

namespace Ethachat.Client.Services.UserAgentService;

public interface IUserAgentService
{
    public Task<UserAgentInformation> GetUserAgentInformation();
}