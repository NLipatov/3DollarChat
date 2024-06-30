using Client.Application.Cryptography.KeyStorage;
using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.ClientOnlyModels.Events;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Strategies.ReceiveStrategies;

public class AesKeyMessageReceivedStrategy(IKeyStorage keyStorage, IMessageService messageService, ICallbackExecutor callbackExecutor) : ITransferHandler<KeyMessage>
{
    public async Task HandleAsync(KeyMessage eventMessage)
    {
        var aesKey = eventMessage.Key;

        await messageService.SendMessage(new EventMessage
        {
            Type = EventType.AesOfferAccepted,
            Sender = eventMessage.Target,
            Target = eventMessage.Sender,
            Id = aesKey.Id
        });

        aesKey.Contact = eventMessage.Sender;
        aesKey.Author = eventMessage.Sender;
        aesKey.IsAccepted = true;
        await keyStorage.StoreAsync(aesKey);

        callbackExecutor.ExecuteSubscriptionsByName(eventMessage.Sender, "OnPartnerAESKeyReady");
        callbackExecutor.ExecuteSubscriptionsByName(eventMessage.Sender, "AESUpdated");
    }
}