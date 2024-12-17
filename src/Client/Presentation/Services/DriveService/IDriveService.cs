using Client.Transfer.Domain.TransferedEntities.Messages;

namespace Ethachat.Client.Services.DriveService;

public interface IDriveService
{
    Task<bool> IsAccessibleAsync();
    Task<byte[]> DownloadAsync(DriveStoredFileMessage message);
    Task<Guid> UploadAsync(byte[] data);
}