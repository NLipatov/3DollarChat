using Ethachat.Client.ClientOnlyModels;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService
{
    public interface IMessageService : IHubService
    {
        Task NegotiateOnAESAsync(string partnerUsername);
        Task SendTypingEventToPartnerAsync(string sender, string receiver);
        Task SendMessage(ClientMessage message);
        Task NotifySenderThatMessageWasRead(Guid messageId, string messageSender, string myUsername);
    }
}
