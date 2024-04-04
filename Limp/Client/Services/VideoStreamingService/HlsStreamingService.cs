using Ethachat.Client.Services.VideoStreamingService.Converters.FFmpeg;
using Ethachat.Client.Services.VideoStreamingService.Extensions;
using EthachatShared.Models.Message;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace Ethachat.Client.Services.VideoStreamingService;

public class HlsStreamingService : IHlsStreamingService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly NavigationManager _navigationManager;

    public HlsStreamingService(IJSRuntime jsRuntime, NavigationManager navigationManager)
    {
        _jsRuntime = jsRuntime;
        _navigationManager = navigationManager;
    }

    public async Task<bool> CanFileBeStreamedAsync(string filename)
    {
        var endpointAddress = string.Join("", _navigationManager.BaseUri, "hlsapi/health");
        using var client = new HttpClient();
        var request = await client.GetAsync(endpointAddress);
        var hlsServiceHealth = await request.Content.ReadAsStringAsync();
            
        return IsExtensionSupportedByHls(filename) && hlsServiceHealth == "Ready";
    }

    private bool IsExtensionSupportedByHls(string filename)
    {
        var extension = Path.GetExtension(filename).ToUpper().Replace('.', ' ');
        if (Enum.TryParse<ExtentionType>(extension, out _))
            return true;
        
        return false;
    }

    public async Task<HlsPlaylist> ToM3U8Async(IBrowserFile browserFile)
    {
        if (!IsExtensionSupportedByHls(browserFile.Name))
            throw new ArgumentException($"Extension is not supported: {browserFile.Name}.");
        
        var extension = Path.GetExtension(browserFile.Name).ToUpper().Replace('.', ' ').Trim();
        if (!Enum.TryParse<ExtentionType>(extension, out var type))
            throw new ArgumentException($"Extension is not supported: {extension}.");
        
        using var memoryStream = new MemoryStream();
        await browserFile
            .OpenReadStream(long.MaxValue)
            .CopyToAsync(memoryStream);
        
        await using var ffmpeg = new FfmpegConverter(_jsRuntime, _navigationManager);
        return type switch
        {
            ExtentionType.MP4 => await ffmpeg.Mp4ToM3U8(memoryStream.ToArray()),
            _ => await ffmpeg.Mp4ToM3U8(await ConvertToMp4(memoryStream.ToArray(), type))
        };
    }

    private async Task<byte[]> ConvertToMp4(byte[] bytes, ExtentionType type)
    {
        await using var ffmpeg = new FfmpegConverter(_jsRuntime, _navigationManager);
        return await ffmpeg.ConvertToMp4(bytes, type);
    }
}