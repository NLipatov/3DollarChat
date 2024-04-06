using Microsoft.AspNetCore.SignalR;

namespace Ethachat.Server.Hubs.MessageDispatcher.Handlers.MessageMarker
{
    public interface IMessageMarker
    {
        Task MarkAsReceived(Guid messageId, string topicName, IHubCallerClients clients);
        Task MarkAsReaded(Guid messageId, string messageSender, IHubCallerClients clients);
    }
}