namespace Ethachat.Client.Services.VideoStreamingService.Converters.FFmpeg.Models;

public class TsFile
{
    public required string Name { get; set; }
    public required byte[] Content { get; set; }
}