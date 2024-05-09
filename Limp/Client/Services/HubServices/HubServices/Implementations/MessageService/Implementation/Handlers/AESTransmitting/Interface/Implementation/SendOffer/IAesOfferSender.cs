using EthachatShared.Encryption;
using EthachatShared.Models.Message;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.
    AESTransmitting.Interface.Implementation.SendOffer;

public interface IAesOfferSender
{
    Task<Message> GenerateAesOfferAsync(string partnersUsername, string partnersPublicKey, Key aesKey);
}