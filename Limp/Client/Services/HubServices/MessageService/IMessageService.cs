using Limp.Client.Services.HubServices.HubServiceContract;
using LimpShared.Models.Message;

namespace Limp.Client.Services.HubServices.MessageService
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
