using Client.Application.Cryptography.KeyStorage;
using Client.Transfer.Domain.Entities.Events;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;
using EthachatShared.Models.Message.KeyTransmition;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Strategies.ReceiveStrategies;

public class OnReceivedAesOffer(IKeyStorage keyStorage, IMessageService messageService, ICallbackExecutor callbackExecutor) : ITransferHandler<AesOffer>
{
    public async Task HandleAsync(AesOffer aesOfferMessage)
    {
        aesOfferMessage.key.IsAccepted = true; 
        aesOfferMessage.key.Contact = aesOfferMessage.key.Author;
        await keyStorage.StoreAsync(aesOfferMessage.key);

        callbackExecutor.ExecuteSubscriptionsByName(aesOfferMessage.Sender, "OnPartnerAESKeyReady");
        callbackExecutor.ExecuteSubscriptionsByName(aesOfferMessage.Sender, "AESUpdated");
        
        await messageService.TransferAsync(new EventMessage
        {
            Id = aesOfferMessage.key.Id,
            Sender = aesOfferMessage.Target,
            Target = aesOfferMessage.Sender,
            Type = EventType.AesOfferAccepted
        });
    }
}