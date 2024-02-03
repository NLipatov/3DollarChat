using EthachatShared.Models.Message;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.AESTransmitting.Interface;

public interface IAesTransmissionManager
{
    Task<Message> GenerateOffer(string partnersUsername, string partnersPublicKey, string aesKey);
    Task<Message> GenerateOfferResponse(Message incomingOffer);
}