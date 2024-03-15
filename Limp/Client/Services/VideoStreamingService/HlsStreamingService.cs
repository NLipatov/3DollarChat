using Ethachat.Client.Services.VideoStreamingService.Converters.FFmpeg;
using Ethachat.Client.Services.VideoStreamingService.FileTypes;
using EthachatShared.Models.Message.VideoStreaming;
using Microsoft.JSInterop;

namespace Ethachat.Client.Services.VideoStreamingService;

public class HlsStreamingService : IHlsStreamingService
{
    private readonly IJSRuntime _jsRuntime;

    public HlsStreamingService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<HlsVideoStreamingDetails> ToM3U8Async(byte[] mp4, ExtentionType type)
    {
        var ffmpeg = new FfmpegConverter(_jsRuntime);
        
        return type switch
        {
            ExtentionType.MP4 => await ffmpeg.ConvertMp4ToM3U8(mp4),
            _ => throw new ArgumentException($"{type} is not supported.")
        };
    }
}