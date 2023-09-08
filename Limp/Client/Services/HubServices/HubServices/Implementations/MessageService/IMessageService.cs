using LimpShared.Models.Message;

namespace Limp.Client.Services.HubServices.HubServices.Implementations.MessageService
{
    public interface IMessageService : IHubService
    {
        Task ReconnectAsync();
        bool IsConnected();
        Task SendMessage(Message message);
        Task NegotiateOnAESAsync(string partnerUsername); 
        Task SendUserMessage(string text, string targetGroup, string myUsername);
        Task NotifySenderThatMessageWasReaded(Guid messageId, string messageSender, string myUsername);
    }
}
