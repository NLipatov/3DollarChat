using EthachatShared.Models.Message;
using EthachatShared.Models.Message.Interfaces;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService
{
    public interface IMessageService : IHubService
    {
        Task NegotiateOnAESAsync(string partnerUsername);
        Task UnsafeTransferAsync(ClientToClientData data);
        Task TransferAsync<T>(T data) where T : IIdentifiable, ISourceResolvable, IDestinationResolvable;
        void PreventReconnecting();
        Task ReconnectAsync();
    }
}
