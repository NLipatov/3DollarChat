using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;
using Ethachat.Client.Services.VideoStreamingService.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Ethachat.Client.Services.VideoStreamingService;

public class HlsStreamingService : IHlsStreamingService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly NavigationManager _navigationManager;
    private readonly ICallbackExecutor _callbackExecutor;

    public HlsStreamingService(IJSRuntime jsRuntime, NavigationManager navigationManager, ICallbackExecutor callbackExecutor)
    {
        _jsRuntime = jsRuntime;
        _navigationManager = navigationManager;
        _callbackExecutor = callbackExecutor;
    }

    public async Task<bool> CanFileBeStreamedAsync(string filename)
    {
        try
        {
            //Is extension supported by HLS?
            if (!IsExtensionSupportedByHls(filename))
                return false;
           
            //Is HLS service accessible?
            var endpointAddress = string.Join("", _navigationManager.BaseUri, "hlsapi/health");
            using var client = new HttpClient();
            var request = await client.GetAsync(endpointAddress);
            var hlsServiceHealth = await request.Content.ReadAsStringAsync();
            var hlsIsAccessible = hlsServiceHealth == "Ready";
            
            return hlsIsAccessible;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return false;
        }
    }

    private bool IsExtensionSupportedByHls(string filename)
    {
        var extension = GetExtensionCodeName(filename);
        
        if (Enum.TryParse<ExtentionType>(extension, out var parsedExtension))
        {
            if (parsedExtension is not ExtentionType.NONE)
            {
                return true;
            }
        }

        return false;
    }

    private string GetExtensionCodeName(string filename)
    {
        return Path.GetExtension(filename)
            .Replace('.', ' ')
            .Replace("3", "_3")
            .ToUpper()
            .Trim();
    }
}