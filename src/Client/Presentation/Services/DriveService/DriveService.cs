using Client.Application.Cryptography;
using Client.Application.Cryptography.KeyStorage;
using Client.Infrastructure.Cryptography.Handlers;
using Client.Transfer.Domain.Entities.Messages;
using EthachatShared.Encryption;
using EthachatShared.Models.Cryptograms;
using Microsoft.AspNetCore.Components;

namespace Ethachat.Client.Services.DriveService;

public class DriveService(NavigationManager navigationManager, IKeyStorage keyStorage, ICryptographyService cryptographyService) : IDriveService
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
        
        var aesKey = await keyStorage.GetLastAcceptedAsync(message.Sender, KeyType.Aes);

        var cryptogram = await cryptographyService.DecryptAsync<AesHandler>(new BinaryCryptogram
        {
            Cypher = data,
            KeyId = message.KeyId,
            Iv = message.Iv,
            EncryptionKeyType = KeyType.Aes
        }, aesKey ?? throw new ApplicationException("Missing key"));

        return cryptogram.Cypher;
    }
}