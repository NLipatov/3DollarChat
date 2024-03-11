using System.Text;
using Microsoft.JSInterop;

namespace Ethachat.Client.Services.VideoStreamingService;

public class m3u8Service : Im3u8Service
{
    private readonly IJSRuntime _jsRuntime;

    public m3u8Service(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }
    public async Task<string> GenerateM3U8Url(List<byte[]> sortedTransportStreamBytes)
    {
        StringBuilder m3u8Content = new StringBuilder();
        
        m3u8Content.AppendLine("#EXTM3U");
        m3u8Content.AppendLine("#EXT-X-VERSION:3");
        m3u8Content.AppendLine("#EXT-X-TARGETDURATION:5");
        m3u8Content.AppendLine("#EXT-X-MEDIA-SEQUENCE:0");
        m3u8Content.AppendLine("#EXTINF:5.280000,");
        foreach (var ts in sortedTransportStreamBytes)
        {
            var tsLink = await GenerateTsUrl(ts);
            m3u8Content.AppendLine(tsLink);
        }
        m3u8Content.AppendLine("#EXT-X-ENDLIST");
        
        var m3U8Link = await _jsRuntime.InvokeAsync<string>("createBlobUrl", Encoding.UTF8.GetBytes(m3u8Content.ToString()));
        return m3U8Link;
    }

    private async Task<string> GenerateTsUrl(byte[] tsContent)
    {
        return await _jsRuntime.InvokeAsync<string>("createBlobUrl", tsContent, "video/mp2t");
    }
}