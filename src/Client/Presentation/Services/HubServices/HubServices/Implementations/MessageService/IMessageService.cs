using Ethachat.Client.ClientOnlyModels;
using EthachatShared.Models.Message.Interfaces;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService
{
    public interface IMessageService : IHubService
    {
        Task NegotiateOnAESAsync(string partnerUsername);
        Task TransferAsync<T>(T data) where T : IIdentifiable, ISourceResolvable, IDestinationResolvable;
        Task SendMessage<T>(T message) where T : IDestinationResolvable;
        Task SendMessage(KeyMessage message);
        void RemoveFromReceived(string rsaKey);
    }
}
