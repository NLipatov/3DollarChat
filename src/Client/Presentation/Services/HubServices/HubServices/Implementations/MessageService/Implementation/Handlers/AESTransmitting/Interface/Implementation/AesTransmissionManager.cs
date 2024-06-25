using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.
    AESTransmitting.Interface.Implementation.SendOffer;
using EthachatShared.Encryption;
using EthachatShared.Models.Message.KeyTransmition;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.
    AESTransmitting.Interface.Implementation;

public class AesTransmissionManager : IAesTransmissionManager
{
    private readonly IAesOfferSender _offerSender;

    public AesTransmissionManager(IAesOfferSender offerSender)
    {
        _offerSender = offerSender;
    }

    public async Task<AesOffer> GenerateOffer(string partnersUsername, Key aesKey) =>
        await _offerSender.GenerateAesOfferAsync(partnersUsername, aesKey);
}