using Client.Application.Cryptography.KeyStorage;
using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.ClientOnlyModels.Events;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Strategies.ReceiveStrategies;

public class AesKeyMessageReceivedStrategy(IKeyStorage keyStorage, IMessageService messageService, ICallbackExecutor callbackExecutor) : ITransferHandler<KeyMessage>
{
    public async Task HandleAsync(KeyMessage keyMessage)
    {
        var aesKey = keyMessage.Key;

        await messageService.SendMessage(new EventMessage
        {
            Type = EventType.AesOfferAccepted,
            Sender = keyMessage.Target,
            Target = keyMessage.Sender,
            Id = aesKey.Id
        });

        aesKey.Contact = keyMessage.Sender;
        aesKey.Author = keyMessage.Sender;
        aesKey.IsAccepted = true;
        await keyStorage.StoreAsync(aesKey);

        callbackExecutor.ExecuteSubscriptionsByName(keyMessage.Sender, "OnPartnerAESKeyReady");
        callbackExecutor.ExecuteSubscriptionsByName(keyMessage.Sender, "AESUpdated");
    }
}