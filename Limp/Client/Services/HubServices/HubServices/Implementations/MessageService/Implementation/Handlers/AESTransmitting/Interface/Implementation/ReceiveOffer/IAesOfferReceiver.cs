using EthachatShared.Models.Message;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.
    AESTransmitting.Interface.Implementation.ReceiveOffer
{
    public interface IAesOfferReceiver
    {
        Task<Message> ReceiveAesOfferAsync(Message offerMessage);
    }
}