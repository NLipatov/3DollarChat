using Microsoft.JSInterop;

namespace Limp.Client.HubInteraction.Handlers.Helpers;

public static class JWTHelper
{
    public static async Task<string?> GetAccessTokenAsync(IJSRuntime jSRuntime)
         => await jSRuntime.InvokeAsync<string>("localStorage.getItem", "access-token");

    public static async Task<string?> GetRefreshTokenAsync(IJSRuntime jSRuntime)
        => await jSRuntime.InvokeAsync<string>("localStorage.getItem", "refresh-token");
}
