using Client.Application.Cryptography;
using Client.Application.Cryptography.KeyStorage;
using Client.Infrastructure.Cryptography.Handlers;
using Client.Transfer.Domain.Entities.Messages;
using Ethachat.Client.Services.DriveService;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.Extensions;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Handlers;
using Ethachat.Client.Services.InboxService;
using EthachatShared.Encryption;
using EthachatShared.Models.Cryptograms;
using Microsoft.JSInterop;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Strategies.ReceiveStrategies;

public class OnReceivedDriveStoredFileMessage(IMessageBox messageBox, IJSRuntime jsRuntime, IDriveService driveService, IKeyStorage keyStorage, ICryptographyService cryptographyService) : IStrategyHandler<DriveStoredFileMessage>
{
    public async Task HandleAsync(DriveStoredFileMessage message)
    {
        var aesKey = await keyStorage.GetLastAcceptedAsync(message.Sender, KeyType.Aes);
        var data = await driveService.DownloadAsync(message);

        var cryptogram = await cryptographyService.DecryptAsync<AesHandler>(new BinaryCryptogram
        {
            Cypher = data,
            KeyId = message.KeyId,
            Iv = message.Iv,
            EncryptionKeyType = KeyType.Aes
        }, aesKey ?? throw new ApplicationException("Missing key"));

        messageBox.AddMessage(await message.ToClientMessage(cryptogram.Cypher, jsRuntime));
    }
}