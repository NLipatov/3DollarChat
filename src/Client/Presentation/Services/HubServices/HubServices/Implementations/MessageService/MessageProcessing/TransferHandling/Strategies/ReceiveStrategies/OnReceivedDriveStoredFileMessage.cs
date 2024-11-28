using Client.Application.Cryptography;
using Client.Application.Cryptography.KeyStorage;
using Client.Infrastructure.Cryptography.Handlers;
using Client.Transfer.Domain.Entities.Messages;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.Extensions;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Handlers;
using Ethachat.Client.Services.InboxService;
using EthachatShared.Encryption;
using EthachatShared.Models.Cryptograms;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Strategies.ReceiveStrategies;

public class OnReceivedDriveStoredFileMessage(IMessageBox messageBox, IJSRuntime jsRuntime, NavigationManager navigationManager, IKeyStorage keyStorage, ICryptographyService cryptographyService) : IStrategyHandler<DriveStoredFileMessage>
{
    public async Task HandleAsync(DriveStoredFileMessage message)
    {
        var data = await LoadMessageDataAsync(message);
        messageBox.AddMessage(await message.ToClientMessage(data, jsRuntime));
    }
    
    private async Task<byte[]> LoadMessageDataAsync(DriveStoredFileMessage message)
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