using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.AESTransmitting.Interface;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.AESTransmitting.ReceiveOffer;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.AESTransmitting.SendOffer;
using EthachatShared.Models.Message;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.AESTransmitting;

public class AesTransmissionManager : IAesTransmissionManager
{
    private readonly IAesOfferReceiver _offerReceiver;
    private readonly IAesOfferSender _offerSender;

    public AesTransmissionManager(IAesOfferReceiver offerReceiver, IAesOfferSender offerSender)
    {
        _offerReceiver = offerReceiver;
        _offerSender = offerSender;
    }

    public async Task<Message> GenerateOffer(string partnersUsername, string partnersPublicKey, string aesKey) =>
        await _offerSender.SendAesOfferAsync(partnersUsername, partnersPublicKey, aesKey);

    public async Task<Message> GenerateOfferResponse(Message incomingOffer) =>
        await _offerReceiver.ReceiveAesOfferAsync(incomingOffer);
}