using Limp.Client.Services.LocalStorageService;
using Limp.Client.Services.UserAgent.Models;
using Limp.Client.Services.UserAgentService;
using Microsoft.JSInterop;

namespace Limp.Client.Services.UserAgent.Implementation;

public class UserAgentService : IUserAgentService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILocalStorageService _localStorageService;
    private const string UserAgentDescriptionKey = "navigator.userAgent";
    private const string UserAgentIdKey = "userAgentId";

    public UserAgentService(IJSRuntime jsRuntime, ILocalStorageService localStorageService)
    {
        _jsRuntime = jsRuntime;
        _localStorageService = localStorageService;
    }

    public async Task<UserAgentInformation> GetUserAgentInformation()
    {
        var userAgentDescription = await _jsRuntime
            .InvokeAsync<string?>("eval","navigator.userAgent");
        
        var userAgentIdString = await _localStorageService.ReadPropertyAsync(UserAgentIdKey);

        if (string.IsNullOrWhiteSpace(userAgentIdString))
        {
            userAgentIdString = Guid.NewGuid().ToString();
            await _localStorageService.WritePropertyAsync(UserAgentIdKey, userAgentIdString);
        }

        return new UserAgentInformation
        {
            UserAgentDescription = userAgentDescription,
            UserAgentId = Guid.Parse(userAgentIdString)
        };
    }
}