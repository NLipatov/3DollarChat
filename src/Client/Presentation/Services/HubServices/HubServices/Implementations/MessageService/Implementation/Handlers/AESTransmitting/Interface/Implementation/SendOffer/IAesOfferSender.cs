using EthachatShared.Encryption;
using EthachatShared.Models.Message.KeyTransmition;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.
    AESTransmitting.Interface.Implementation.SendOffer;

public interface IAesOfferSender
{
    Task<AesOffer> GenerateAesOfferAsync(string partnersUsername, Key aesKey);
}