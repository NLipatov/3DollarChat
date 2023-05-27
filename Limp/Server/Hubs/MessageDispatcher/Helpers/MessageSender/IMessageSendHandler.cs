using ClientServerCommon.Models.Message;
using Microsoft.AspNetCore.SignalR;

namespace Limp.Server.Hubs.MessageDispatcher.Helpers.MessageSender
{
    public interface IMessageSendHandler
    {
        Task MarkAsReceived(Guid messageId, string topicName, IHubCallerClients clients);
        Task SendAsync(Message message, IHubCallerClients clients);
    }
}