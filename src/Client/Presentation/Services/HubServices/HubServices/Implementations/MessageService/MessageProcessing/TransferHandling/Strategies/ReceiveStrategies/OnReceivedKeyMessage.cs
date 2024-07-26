using Client.Application.Cryptography;
using Client.Application.Cryptography.KeyStorage;
using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.AESTransmitting.Interface;
using EthachatShared.Encryption;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Strategies.ReceiveStrategies;

public class OnReceivedKeyMessage(IKeyStorage keyStorage, IMessageService messageService, ICryptographyService cryptographyService, IAesTransmissionManager aesTransmissionManager) : ITransferHandler<KeyMessage>
{
    public async Task HandleAsync(KeyMessage message)
    {
        var handleTask = message.Key.Type switch
        {
            KeyType.RsaPublic => HandleRsaPublicReceivedAsync(message),
            _ => throw new ArgumentException($"Unexpected {nameof(message.Key.Type)} - {message.Key.Type}")
        };

        await handleTask;
    }

    private async Task HandleRsaPublicReceivedAsync(KeyMessage message)
    {
        message.Key.Author = message.Sender;
        message.Key.Contact = message.Sender;
        await keyStorage.StoreAsync(message.Key);
        
        var aesKey = await cryptographyService.GenerateAesKeyAsync(message.Sender);
        aesKey.Author = message.Target;

        var offer = await aesTransmissionManager.GenerateOffer(message.Sender, aesKey);
        await messageService.TransferAsync(offer);
    }
}