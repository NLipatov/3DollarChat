using Ethachat.Client.Services.VideoStreamingService.Converters.FFmpeg;
using Ethachat.Client.Services.VideoStreamingService.FileTypes;
using EthachatShared.Models.Message;
using Microsoft.AspNetCore.Components;
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

    public async Task<HlsPlaylist> ToM3U8Async(byte[] mp4, ExtentionType type)
    {
        await using (var ffmpeg = new FfmpegConverter(_jsRuntime, _navigationManager))
        {
            return type switch
            {
                ExtentionType.MP4 => await ffmpeg.Mp4ToM3U8(mp4),
                _ => throw new ArgumentException($"{type} is not supported.")
            };
        }
    }
}