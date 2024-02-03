using Ethachat.Client.ClientOnlyModels;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService
{
    public interface IMessageService : IHubService
    {
        bool IsConnected();
        Task NegotiateOnAESAsync(string partnerUsername);
        Task SendMessage(ClientMessage message);
        Task RequestPartnerToDeleteConvertation(string targetGroup);
        Task NotifySenderThatMessageWasReaded(Guid messageId, string messageSender, string myUsername);
    }
}
