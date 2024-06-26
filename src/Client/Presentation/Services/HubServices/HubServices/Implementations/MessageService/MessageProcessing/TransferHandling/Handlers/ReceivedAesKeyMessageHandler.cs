using Client.Application.Cryptography.KeyStorage;
using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.ClientOnlyModels.Events;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Handlers;

public class ReceivedAesKeyMessageHandler(IKeyStorage keyStorage, IMessageService messageService, ICallbackExecutor callbackExecutor) : ITransferHandler<KeyMessage>
{
    public async Task HandleAsync(KeyMessage clientMessage)
    {
        var aesKey = clientMessage.Key;
        aesKey.Contact = clientMessage.Sender;
        aesKey.Author = clientMessage.Sender;
        await keyStorage.StoreAsync(aesKey);

        await messageService.SendMessage(new EventMessage
        {
            Type = EventType.AesOfferAccepted,
            Sender = clientMessage.Target,
            Target = clientMessage.Sender,
            Id = aesKey.Id
        });

        callbackExecutor.ExecuteSubscriptionsByName(clientMessage.Sender, "OnPartnerAESKeyReady");
        callbackExecutor.ExecuteSubscriptionsByName(clientMessage.Sender, "AESUpdated");
    }
}