using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Ethachat.Client.Services.DriveService;

public class DriveService(NavigationManager navigationManager) : IDriveService
{
    public async Task<bool> CanFileTransmittedAsync(IBrowserFile browserFile)
    {
        //Is Drive service accessible?
        var endpointAddress = string.Join("", navigationManager.BaseUri, "driveapi/health");
        using var client = new HttpClient();
        var request = await client.GetAsync(endpointAddress);
        var hlsServiceHealth = await request.Content.ReadAsStringAsync();
        var hlsIsAccessible = hlsServiceHealth == "OK";
            
        return hlsIsAccessible;
    }
}