namespace Ethachat.Client.Services.VideoStreamingService;

public interface Im3u8Service
{
    Task<string> GenerateM3U8Url(List<byte[]> sortedTransportStreamBytes);
}