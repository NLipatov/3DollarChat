using Client.Transfer.Domain.Entities.Messages;

namespace Ethachat.Client.Services.DriveService;

public interface IDriveService
{
    Task<bool> IsAccessibleAsync();
    Task<byte[]> DownloadAsync(DriveStoredFileMessage message);
    Task<Guid> UploadAsync(byte[] data);
}