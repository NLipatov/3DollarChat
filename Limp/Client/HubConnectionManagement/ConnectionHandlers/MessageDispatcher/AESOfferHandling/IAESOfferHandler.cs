using ClientServerCommon.Models.Message;

namespace Limp.Client.HubConnectionManagement.ConnectionHandlers.MessageDispatcher.AESOfferHandling
{
    public interface IAESOfferHandler
    {
        Task<Message> GetAESOfferResponse(Message offerMessage);
    }
}
