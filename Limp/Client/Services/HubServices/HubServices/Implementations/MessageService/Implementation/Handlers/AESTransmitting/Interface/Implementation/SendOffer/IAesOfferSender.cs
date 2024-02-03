using EthachatShared.Models.Message;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.
    AESTransmitting.Interface.Implementation.SendOffer;

public interface IAesOfferSender
{
    Task<Message> SendAesOfferAsync(string partnersUsername, string partnersPublicKey, string aesKey);
}