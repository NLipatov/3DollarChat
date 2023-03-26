using ClientServerCommon.Models.Message;

namespace Limp.Client.HubInteraction.Handlers.MessageDispatcherHub.AESOfferHandling
{
    public interface IAESOfferHandler
    {
        Task<Message> GetAESOfferResponse(Message offerMessage);
    }
}
