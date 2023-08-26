using Limp.Client.Services.UserAgentService.Models;

namespace Limp.Client.Services.UserAgentService;

public interface IUserAgentService
{
    public Task<UserAgentInformation> GetUserAgentInformation();
}