using Client.Application.Cryptography;
using Client.Application.Cryptography.KeyStorage;
using Client.Transfer.Domain.Entities.Events;
using Client.Transfer.Domain.Entities.Messages;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;
using EthachatShared.Encryption;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.
    TransferHandling.Strategies.ReceiveStrategies;

public class OnReceivedKeyMessage(
    IKeyStorage keyStorage,
    IMessageService messageService,
    ICryptographyService cryptographyService,
    ICallbackExecutor callbackExecutor) : ITransferHandler<KeyMessage>
{
    private readonly HashSet<Guid> _handledIds = [];

    public async Task HandleAsync(KeyMessage message)
    {
        if (!_handledIds.Add(message.Id)) //if .Add returned true - event message is new, else - duplicate
            return;

        var handleTask = message.Key.Type switch
        {
            KeyType.Aes => HandleAesReceivedAsync(message),
            KeyType.RsaPublic => HandleRsaPublicReceivedAsync(message),
            _ => throw new ArgumentException($"Unexpected {nameof(message.Key.Type)} - {message.Key.Type}")
        };

        await handleTask;
    }

    private async Task HandleAesReceivedAsync(KeyMessage message)
    {
        message.Key.IsAccepted = true; 
        message.Key.Contact = message.Sender;
        await keyStorage.StoreAsync(message.Key);

        callbackExecutor.ExecuteSubscriptionsByName(message.Sender, "OnPartnerAESKeyReady");
        callbackExecutor.ExecuteSubscriptionsByName(message.Sender, "AESUpdated");
        
        await messageService.TransferAsync(new EventMessage
        {
            Id = message.Key.Id,
            Sender = message.Target,
            Target = message.Sender,
            Type = EventType.AesOfferAccepted
        });
    }

    private async Task HandleRsaPublicReceivedAsync(KeyMessage message)
    {
        message.Key.Author = message.Sender;
        message.Key.Contact = message.Sender;
        await keyStorage.StoreAsync(message.Key);

        var keyMessage = await GenerateAesOfferAsync(message);
        await keyStorage.StoreAsync(keyMessage.Key);
        await messageService.TransferAsync(keyMessage);
    }

    private async Task<KeyMessage> GenerateAesOfferAsync(KeyMessage keyMessage)
    {
        var aesKey = await cryptographyService.GenerateAesKeyAsync(keyMessage.Sender, keyMessage.Target);

        return new()
        {
            Id = aesKey.Id,
            Sender = keyMessage.Target,
            Target = keyMessage.Sender,
            Key = aesKey,
        };
    }
}