using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;
using Ethachat.Client.Services.VideoStreamingService.Converters.FFmpeg;
using Ethachat.Client.Services.VideoStreamingService.Converters.FFmpeg.Ffmpeginitialization;
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
            //Can ff be loaded? Is there enough memory on client device?
            FfmpegInitializationManager ffmpegInitializationManager = new();
            await ffmpegInitializationManager.InitializeAsync(_jsRuntime);
        
            //Is HLS service can be accessed?
            var endpointAddress = string.Join("", _navigationManager.BaseUri, "hlsapi/health");
            using var client = new HttpClient();
            var request = await client.GetAsync(endpointAddress);
            var hlsServiceHealth = await request.Content.ReadAsStringAsync();
            var hlsIsHealthy = hlsServiceHealth == "Ready";
            
            //Is extension supported by HLS?
            var extensionIsSupported = IsExtensionSupportedByHls(filename); 
            return extensionIsSupported && hlsIsHealthy;
        }
        catch (Exception e)
        {
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

    public async Task<HlsPlaylist> ToM3U8Async(IBrowserFile browserFile)
    {
        try
        {
            if (!IsExtensionSupportedByHls(browserFile.Name))
                throw new ArgumentException($"Extension is not supported: {browserFile.Name}.");
        
            var extension = GetExtensionCodeName(browserFile.Name);;
            if (!Enum.TryParse<ExtentionType>(extension, out var type))
                throw new ArgumentException($"Extension is not supported: {extension}.");
        
            using var memoryStream = new MemoryStream();
            await browserFile
                .OpenReadStream(long.MaxValue)
                .CopyToAsync(memoryStream);
            
            await using var ffmpeg = new FfmpegConverter(_jsRuntime, _navigationManager, _callbackExecutor);
            return type switch
            {
                ExtentionType.MP4 => await ffmpeg.Mp4ToM3U8(memoryStream.ToArray()),
                _ => await ffmpeg.Mp4ToM3U8(await ConvertToMp4(memoryStream.ToArray(), type))
            };
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw new ArgumentException($"Exception occured in {nameof(ToM3U8Async)} method: {e.Message}.");
        }
    }

    private async Task<byte[]> ConvertToMp4(byte[] bytes, ExtentionType type)
    {
        await using var ffmpeg = new FfmpegConverter(_jsRuntime, _navigationManager, _callbackExecutor);
        return await ffmpeg.ConvertToMp4(bytes, type);
    }
}