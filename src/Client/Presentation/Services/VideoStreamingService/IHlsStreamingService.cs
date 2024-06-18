namespace Ethachat.Client.Services.VideoStreamingService;

public interface IHlsStreamingService
{
    Task<bool> CanFileBeStreamedAsync(string filename);
}