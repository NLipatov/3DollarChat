using Limp.Client.Services.UserAgentService.Models;
using Microsoft.JSInterop;

namespace Limp.Client.Services.UserAgentService.Implementation;

public class UserAgentService : IUserAgentService
{
    private readonly IJSRuntime _jsRuntime;

    public UserAgentService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<UserAgentInformation> GetUserAgentInformation()
    {
        var userAgentDescription = await _jsRuntime
            .InvokeAsync<string?>("eval","navigator.userAgent");
        
        var userAgentId = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "userAgent");
        if (string.IsNullOrWhiteSpace(userAgentId))
        {
            userAgentId = Guid.NewGuid().ToString();
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "userAgent", userAgentId);
        }

        return new UserAgentInformation
        {
            UserAgentDescription = userAgentDescription,
            UserAgentId = Guid.Parse(userAgentId)
        };
    }
}