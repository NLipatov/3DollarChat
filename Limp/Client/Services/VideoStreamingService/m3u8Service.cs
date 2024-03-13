using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;
using Ethachat.Client.Services.VideoStreamingService.Converters.FFmpeg;
using Ethachat.Client.Services.VideoStreamingService.FileTypes;
using Microsoft.JSInterop;

namespace Ethachat.Client.Services.VideoStreamingService;

public class M3U8Service : Im3u8Service
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ICallbackExecutor _callbackExecutor;

    public M3U8Service(IJSRuntime jsRuntime, ICallbackExecutor callbackExecutor)
    {
        _jsRuntime = jsRuntime;
        _callbackExecutor = callbackExecutor;
    }

    public async Task ToM3U8Async(byte[] mp4, ExtentionType type)
    {
        var ffmpeg = new FfmpegConverter(_jsRuntime, _callbackExecutor);
        switch (type)
        {
            case ExtentionType.MP4:
                await ffmpeg.ConvertMp4ToM3U8(mp4);
                break;
            default:
                throw new ArgumentException($"{type} is not supported.");
        }
    }
}