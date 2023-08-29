using Limp.Client.Services.UserAgent.Models;

namespace Limp.Client.Services.UserAgentService;

public interface IUserAgentService
{
    public Task<UserAgentInformation> GetUserAgentInformation();
}