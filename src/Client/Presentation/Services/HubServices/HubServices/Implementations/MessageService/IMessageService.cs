using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.ClientOnlyModels.Events;
using EthachatShared.Models.Message.DataTransfer;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService
{
    public interface IMessageService : IHubService
    {
        Task NegotiateOnAESAsync(string partnerUsername);
        Task SendMessage(ClientMessage message);
        Task SendMessage(EventMessage message);
        Task SendMessage(Package message);
        Task SendMessage(KeyMessage message);
        Task SendMessage(HlsPlaylistMessage message);
    }
}
