using EthachatShared.Encryption;
using EthachatShared.Models.Message.KeyTransmition;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.
    AESTransmitting.Interface;

public interface IAesTransmissionManager
{
    Task<AesOffer> GenerateOffer(string partnersUsername, Key aesKey);
}