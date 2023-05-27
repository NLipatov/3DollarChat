using ClientServerCommon.Models.Message;
using Microsoft.AspNetCore.SignalR;

namespace Limp.Server.Hubs.MessageDispatcher.Helpers.MessageSender
{
    public interface IMessageSendHandler
    {
        Task MarkAsReceived(Message message, IHubCallerClients clients);
        Task SendAsync(Message message, IHubCallerClients clients);
    }
}