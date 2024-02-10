using EthachatShared.Models.Message;
using Microsoft.AspNetCore.SignalR;

namespace Ethachat.Server.Hubs.MessageDispatcher.Handlers.MessageSender
{
    public interface IMessageSendHandler
    {
        Task MarkAsReceived(Guid messageId, string topicName, IHubCallerClients clients);
        Task SendAsync(Message message, IHubCallerClients clients);
        Task MarkAsReaded(Guid messageId, string messageSender, IHubCallerClients clients);
    }
}