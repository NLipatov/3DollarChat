using Client.Application.Cryptography.KeyStorage;
using Ethachat.Client.ClientOnlyModels.Events;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;
using EthachatShared.Models.Message.KeyTransmition;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Strategies.ReceiveStrategies;

public class AesOfferReceivedStrategy(IKeyStorage keyStorage, IMessageService messageService, ICallbackExecutor callbackExecutor) : ITransferHandler<AesOffer>
{
    public async Task HandleAsync(AesOffer eventMessage)
    {
        await messageService.SendMessage(new EventMessage
        {
            Id = eventMessage.key.Id,
            Sender = eventMessage.Target,
            Target = eventMessage.Sender,
            Type = EventType.AesOfferAccepted
        });
        
        eventMessage.key.IsAccepted = true; 
        eventMessage.key.Contact = eventMessage.key.Author;
        await keyStorage.StoreAsync(eventMessage.key);

        callbackExecutor.ExecuteSubscriptionsByName(eventMessage.Sender, "OnPartnerAESKeyReady");
        callbackExecutor.ExecuteSubscriptionsByName(eventMessage.Sender, "AESUpdated");
    }
}