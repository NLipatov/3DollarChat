using Ethachat.Client.Services.VideoStreamingService.Converters.FFmpeg.Models;
using Ethachat.Client.Services.VideoStreamingService.FileTypes;

namespace Ethachat.Client.Services.VideoStreamingService;

public interface IHlsStreamingService
{
    Task<HlsPlaylist> ToM3U8Async(byte[] mp4, ExtentionType type);
}