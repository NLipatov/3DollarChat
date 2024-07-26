using Client.Application.Cryptography.KeyStorage;
using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.ClientOnlyModels.Events;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.ContextManagers.
    AesKeyExchange;
using Ethachat.Client.Services.InboxService;
using EthachatShared.Encryption;
using EthachatShared.Models.Cryptograms;
using EthachatShared.Models.Message;
using MessagePack;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.
    TransferHandling.Strategies.ReceiveStrategies;

public class OnReceivedEventMessage(
    IMessageBox messageBox,
    ICallbackExecutor callbackExecutor,
    IMessageService messageService,
    IKeyStorage keyStorage,
    IKeyExchangeContextManager keyExchangeContextManager) : ITransferHandler<EventMessage>
{
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

    private async Task HandleConversationDeletionAsync(EventMessage eventMessage)
    {
        messageBox.Delete(eventMessage.Sender);
    }

    private async Task HandleMessageReadAsync(EventMessage eventMessage)
    {
        callbackExecutor.ExecuteSubscriptionsByName(eventMessage.Id,
            "OnReceiverMarkedMessageAsRead");
    }

    private async Task HandleMessageReceivedAsync(EventMessage eventMessage)
    {
        callbackExecutor.ExecuteSubscriptionsByName(eventMessage.Id,
            "OnReceiverMarkedMessageAsReceived");
    }

    private async Task HandleResendRequestAsync(EventMessage eventMessage)
    {
        var message = messageBox.Messages.FirstOrDefault(x => x.Id == eventMessage.Id);
        if (message?.Type is MessageType.TextMessage)
        {
            await messageService.TransferAsync(message);
        }
    }

    private async Task HandleDataTransferConfirmationAsync(EventMessage eventMessage)
    {
        callbackExecutor.ExecuteSubscriptionsByName(eventMessage.Id, "OnFileReceived");
    }

    private async Task HandleOnTypingAsync(EventMessage eventMessage)
    {
        callbackExecutor.ExecuteSubscriptionsByName(eventMessage.Sender, "OnTyping");
    }

    private async Task HandleAesOfferAcceptedAsync(EventMessage eventMessage)
    {
        var keys = await keyStorage.GetAsync(eventMessage.Sender, KeyType.Aes);
        var acceptedKey = keys.FirstOrDefault(x => x.Id == eventMessage.Id) ??
                          throw new ArgumentException("Missing key");
        acceptedKey.IsAccepted = true;
        await keyStorage.UpdateAsync(acceptedKey);

        callbackExecutor.ExecuteSubscriptionsByName(eventMessage.Sender, "OnPartnerAESKeyReady");
        callbackExecutor.ExecuteSubscriptionsByName(eventMessage.Sender, "AESUpdated");

        var rsaKey =
            ((await keyStorage.GetAsync(eventMessage.Sender, KeyType.RsaPublic)).MaxBy(x => x.CreationDate) ??
             throw new NullReferenceException()).Value.ToString();
        keyExchangeContextManager.Delete(eventMessage.Sender, rsaKey);
    }

    private async Task HandleRsaPubKeyRequestAsync(EventMessage eventMessage)
    {
        var rsaPubKeys = await keyStorage.GetAsync(string.Empty, KeyType.RsaPublic);
        var mostRecentRsa = rsaPubKeys.MaxBy(x => x.CreationDate) ??
                            throw new NullReferenceException("Missing RSA Public Key");

        var keyMessage = new KeyMessage
        {
            Sender = eventMessage.Target,
            Target = eventMessage.Sender,
            Key = mostRecentRsa
        };
        await messageService.UnsafeTransferAsync(new EncryptedDataTransfer
        {
            Id = Guid.NewGuid(),
            Sender = keyMessage.Sender,
            Target = keyMessage.Target,
            DataType = typeof(KeyMessage),
            BinaryCryptogram = new BinaryCryptogram
            {
                Cypher = MessagePackSerializer.Serialize(keyMessage),
                Iv = [],
                KeyId = keyMessage.Key.Id,
                EncryptionKeyType = KeyType.None
            }
        });
    }
}