using Microsoft.AspNetCore.Components.Forms;

namespace Ethachat.Client.Services.DriveService;

public interface IDriveService
{
    Task<bool> CanFileTransmittedAsync(IBrowserFile browserFile);
}