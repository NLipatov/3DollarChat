using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.ClientOnlyModels.Events;
using EthachatShared.Models.Message.Interfaces;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService
{
    public interface IMessageService : IHubService
    {
        Task NegotiateOnAESAsync(string partnerUsername);
        Task TransferAsync<T>(T data) where T : IIdentifiable, ISourceResolvable, IDestinationResolvable;
        Task SendMessage(EventMessage message);
        Task SendMessage<T>(T message) where T : IDestinationResolvable;
        Task SendMessage(KeyMessage message);
    }
}
