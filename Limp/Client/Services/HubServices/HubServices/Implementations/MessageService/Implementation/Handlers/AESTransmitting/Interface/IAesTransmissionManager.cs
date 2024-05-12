using EthachatShared.Encryption;
using EthachatShared.Models.Message;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.
    AESTransmitting.Interface;

public interface IAesTransmissionManager
{
    Task<Message> GenerateOffer(string partnersUsername, string partnersPublicKey, Key aesKey);
    Task<Message> GenerateOfferResponse(Message incomingOffer);
}