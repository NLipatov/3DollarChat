using Client.Application.Cryptography;
using Client.Application.Cryptography.KeyStorage;
using Ethachat.Client.ClientOnlyModels;
using EthachatShared.Encryption;
using EthachatShared.Models.Message.KeyTransmition;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.
    TransferHandling.Strategies.ReceiveStrategies;

public class OnReceivedKeyMessage(
    IKeyStorage keyStorage,
    IMessageService messageService,
    ICryptographyService cryptographyService) : ITransferHandler<KeyMessage>
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

        var offer = await GenerateAesOfferAsync(message);
        await messageService.TransferAsync(offer);
    }

    private async Task<AesOffer> GenerateAesOfferAsync(KeyMessage keyMessage)
    {
        var aesKey = await cryptographyService.GenerateAesKeyAsync(keyMessage.Sender, keyMessage.Target);
        await keyStorage.StoreAsync(aesKey);

        return new()
        {
            Id = aesKey.Id,
            Sender = keyMessage.Target,
            Target = keyMessage.Sender,
            key = aesKey,
        };
    }
}