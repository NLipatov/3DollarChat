using Ethachat.Client.Services.VideoStreamingService.FileTypes;
using EthachatShared.Models.Message;

namespace Ethachat.Client.Services.VideoStreamingService;

public interface IHlsStreamingService
{
    Task<HlsPlaylist> ToM3U8Async(byte[] mp4, ExtentionType type);
}