namespace Ethachat.Client.Services.DriveService;

public interface IDriveService
{
    Task<bool> IsAccessibleAsync();
}