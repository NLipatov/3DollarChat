using Client.Application.Cryptography.KeyStorage;
using Ethachat.Client.ClientOnlyModels.Events;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;
using EthachatShared.Models.Message.KeyTransmition;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Handlers;

public class AesOfferHandler(IKeyStorage keyStorage, IMessageService messageService, ICallbackExecutor callbackExecutor) : ITransferHandler<AesOffer>
{
    public async Task HandleAsync(AesOffer offer)
    {
        offer.key.IsAccepted = true;
        offer.key.Contact = offer.key.Author;
        await keyStorage.StoreAsync(offer.key);
        
        await messageService.SendMessage(new EventMessage
        {
            Id = offer.key.Id,
            Sender = offer.Target,
            Target = offer.Sender,
            Type = EventType.AesOfferAccepted
        });

        callbackExecutor.ExecuteSubscriptionsByName(offer.Sender, "OnPartnerAESKeyReady");
        callbackExecutor.ExecuteSubscriptionsByName(offer.Sender, "AESUpdated");
    }
}