using Ethachat.Client.Services.VideoStreamingService.FileTypes;

namespace Ethachat.Client.Services.VideoStreamingService;

public interface Im3u8Service
{
    Task ToM3U8Async(byte[] mp4, ExtentionType type);
}