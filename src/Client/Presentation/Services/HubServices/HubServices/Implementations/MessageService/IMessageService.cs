using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.ClientOnlyModels.Events;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService
{
    public interface IMessageService : IHubService
    {
        Task NegotiateOnAESAsync(string partnerUsername);
        Task SendMessage(ClientMessage message);
        Task SendMessage(EventMessage message);
    }
}
