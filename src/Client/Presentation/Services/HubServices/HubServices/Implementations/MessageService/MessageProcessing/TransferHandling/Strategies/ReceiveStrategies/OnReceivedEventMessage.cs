using Client.Application.Cryptography.KeyStorage;
using Client.Transfer.Domain.Entities.Events;
using Client.Transfer.Domain.Entities.Messages;
using Ethachat.Client.Extensions;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling
    .Handlers;
using Ethachat.Client.Services.InboxService;
using EthachatShared.Encryption;
using EthachatShared.Models.Cryptograms;
using EthachatShared.Models.Message;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.
    TransferHandling.Strategies.ReceiveStrategies;

public class OnReceivedEventMessage(
    IMessageBox messageBox,
    ICallbackExecutor callbackExecutor,
    IMessageService messageService,
    IKeyStorage keyStorage) : IStrategyHandler<EventMessage>
{
    private readonly HashSet<Guid> _handledIds = [];

    public async Task HandleAsync(EventMessage message)
    {
        var handleTask = message.Type switch
        {
            EventType.ConversationDeletion => HandleConversationDeletionAsync(message),
            EventType.MessageRead => HandleMessageReadAsync(message),
            EventType.MessageReceived => HandleMessageReceivedAsync(message),
            EventType.ResendRequest => HandleResendRequestAsync(message),
            EventType.DataTransferConfirmation => HandleDataTransferConfirmationAsync(message),
            EventType.OnTyping => HandleOnTypingAsync(message),
            EventType.AesOfferAccepted => HandleAesOfferAcceptedAsync(message),
            EventType.RsaPubKeyRequest => HandleRsaPubKeyRequestAsync(message),
            _ => throw new ArgumentException($"Unexpected {nameof(message.Type)} - {message.Type}")
        };

        await handleTask;
    }

    private Task HandleConversationDeletionAsync(EventMessage eventMessage)
    {
        messageBox.Delete(eventMessage.Sender);
        return Task.CompletedTask;
    }

    private Task HandleMessageReadAsync(EventMessage eventMessage)
    {
        callbackExecutor.ExecuteSubscriptionsByName(eventMessage.Id,
            "OnReceiverMarkedMessageAsRead");
        return Task.CompletedTask;
    }

    private Task HandleMessageReceivedAsync(EventMessage eventMessage)
    {
        callbackExecutor.ExecuteSubscriptionsByName(eventMessage.Id,
            "OnReceiverMarkedMessageAsReceived");
        return Task.CompletedTask;
    }

    private async Task HandleResendRequestAsync(EventMessage eventMessage)
    {
        var message = messageBox.Messages.FirstOrDefault(x => x.Id == eventMessage.Id);
        if (message is not null)
            await messageService.TransferAsync(message);
    }

    private Task HandleDataTransferConfirmationAsync(EventMessage eventMessage)
    {
        callbackExecutor.ExecuteSubscriptionsByName(eventMessage.Id, "OnFileReceived");
        return Task.CompletedTask;
    }

    private Task HandleOnTypingAsync(EventMessage eventMessage)
    {
        callbackExecutor.ExecuteSubscriptionsByName(eventMessage.Sender, "OnTyping");
        return Task.CompletedTask;
    }

    private async Task HandleAesOfferAcceptedAsync(EventMessage eventMessage)
    {
        if (!_handledIds.Add(eventMessage.Id)) //if .Add returned true - event message is new, else - duplicate
            return;
        
        var keys = await keyStorage.GetAsync(eventMessage.Sender, KeyType.Aes);
        var acceptedKey = keys.FirstOrDefault(x => x.Id == eventMessage.Id) ??
                          throw new ArgumentException("Missing key");
        acceptedKey.IsAccepted = true;
        await keyStorage.UpdateAsync(acceptedKey);

        callbackExecutor.ExecuteSubscriptionsByName(eventMessage.Sender, "OnPartnerAESKeyReady");
        callbackExecutor.ExecuteSubscriptionsByName(eventMessage.Sender, "AESUpdated");
    }

    private async Task HandleRsaPubKeyRequestAsync(EventMessage eventMessage)
    {
        if (!_handledIds.Add(eventMessage.Id)) //if .Add returned true - event message is new, else - duplicate
            return;
        
        var rsaPubKeys = await keyStorage.GetAsync(string.Empty, KeyType.RsaPublic);
        var mostRecentRsa = rsaPubKeys.MaxBy(x => x.CreationDate) ??
                            throw new NullReferenceException("Missing RSA Public Key");

        var keyMessage = new KeyMessage
        {
            Sender = eventMessage.Target,
            Target = eventMessage.Sender,
            Key = mostRecentRsa
        };
        await messageService.UnsafeTransferAsync(new ClientToClientData
        {
            Id = Guid.NewGuid(),
            Sender = keyMessage.Sender,
            Target = keyMessage.Target,
            DataType = typeof(KeyMessage),
            BinaryCryptogram = new BinaryCryptogram
            {
                Cypher = await keyMessage.SerializeAsync(),
                Iv = [],
                KeyId = keyMessage.Key.Id,
                EncryptionKeyType = KeyType.None
            }
        });
    }
}