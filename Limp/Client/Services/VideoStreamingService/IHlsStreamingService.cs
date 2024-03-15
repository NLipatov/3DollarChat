using Ethachat.Client.Services.VideoStreamingService.FileTypes;
using EthachatShared.Models.Message.VideoStreaming;

namespace Ethachat.Client.Services.VideoStreamingService;

public interface IHlsStreamingService
{
    Task<HlsVideoStreamingDetails> ToM3U8Async(byte[] mp4, ExtentionType type);
}