using EthachatShared.Models.Message;
using Microsoft.AspNetCore.Components.Forms;

namespace Ethachat.Client.Services.VideoStreamingService;

public interface IHlsStreamingService
{
    Task<HlsPlaylist> ToM3U8Async(IBrowserFile browserFile);
    bool IsExtensionSupportedByHLS(string filename);
}