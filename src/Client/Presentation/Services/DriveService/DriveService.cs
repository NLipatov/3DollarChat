using System.Net.Http.Headers;
using Client.Transfer.Domain.Entities.Messages;
using Microsoft.AspNetCore.Components;

namespace Ethachat.Client.Services.DriveService;

public class DriveService(NavigationManager navigationManager) : IDriveService
{
    public async Task<bool> IsAccessibleAsync()
    {
        //Is Drive service accessible?
        var endpointAddress = string.Join("", navigationManager.BaseUri, "driveapi/health");
        using var client = new HttpClient();
        var request = await client.GetAsync(endpointAddress);
        var hlsServiceHealth = await request.Content.ReadAsStringAsync();
        var hlsIsAccessible = hlsServiceHealth == "OK";
            
        return hlsIsAccessible;
    }

    public async Task<byte[]> DownloadAsync(DriveStoredFileMessage message)
    {
        using var httpClient = new HttpClient();
        var getDataUrl = string.Join("", navigationManager.BaseUri, "driveapi/get?id=", message.Id);
        var request = await httpClient.GetAsync(getDataUrl);
        var data = await request.Content.ReadAsByteArrayAsync();

        return data;
    }

    public async Task<Guid> UploadAsync(byte[] data)
    {
        var formData = new MultipartFormDataContent();
        var content = new ByteArrayContent(data);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        
        formData.Add(content, "payload", Guid.NewGuid().ToString());
        var hlsApiUrl = string.Join("", navigationManager.BaseUri, "driveapi/save");

        using var httpClient = new HttpClient();
        var request = await httpClient.PostAsync(hlsApiUrl, formData);

        if (!request.IsSuccessStatusCode)
        {
            var responseText = await request.Content.ReadAsStringAsync();
            throw new ApplicationException($"Could not post video to HLS API. Status code: {request.StatusCode}, Response: {responseText}");
        }

        var responseJson = await request.Content.ReadAsStringAsync();
        if (!Guid.TryParse(responseJson, out var storedFileId))
            throw new ArgumentException("Invalid file ID");

        return storedFileId;
    }
}