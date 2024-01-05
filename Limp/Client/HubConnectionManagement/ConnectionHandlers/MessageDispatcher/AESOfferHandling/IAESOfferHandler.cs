using EthachatShared.Models.Message;

namespace Ethachat.Client.HubConnectionManagement.ConnectionHandlers.MessageDispatcher.AESOfferHandling
{
    public interface IAESOfferHandler
    {
        Task<Message> GetAESOfferResponse(Message offerMessage);
    }
}
