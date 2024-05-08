using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.
    AESTransmitting.Interface.Implementation.ReceiveOffer;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.
    AESTransmitting.Interface.Implementation.SendOffer;
using EthachatShared.Encryption;
using EthachatShared.Models.Message;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.
    AESTransmitting.Interface.Implementation;

public class AesTransmissionManager : IAesTransmissionManager
{
    private readonly IAesOfferReceiver _offerReceiver;
    private readonly IAesOfferSender _offerSender;

    public AesTransmissionManager(IAesOfferReceiver offerReceiver, IAesOfferSender offerSender)
    {
        _offerReceiver = offerReceiver;
        _offerSender = offerSender;
    }

    public async Task<Message> GenerateOffer(string partnersUsername, string partnersPublicKey, Key aesKey) =>
        await _offerSender.SendAesOfferAsync(partnersUsername, partnersPublicKey, aesKey);

    public async Task<Message> GenerateOfferResponse(Message incomingOffer) =>
        await _offerReceiver.ReceiveAesOfferAsync(incomingOffer);
}