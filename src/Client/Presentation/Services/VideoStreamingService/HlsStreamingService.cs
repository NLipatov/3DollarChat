using Ethachat.Client.Services.VideoStreamingService.Extensions;
using Microsoft.AspNetCore.Components;

namespace Ethachat.Client.Services.VideoStreamingService;

public class HlsStreamingService(NavigationManager navigationManager) : IHlsStreamingService
{
    public async Task<bool> CanFileBeStreamedAsync(string filename)
    {
        try
        {
            //Is extension supported by HLS?
            if (!IsExtensionSupportedByHls(filename))
                return false;
           
            //Is HLS service accessible?
            var endpointAddress = string.Join("", navigationManager.BaseUri, "hlsapi/health");
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