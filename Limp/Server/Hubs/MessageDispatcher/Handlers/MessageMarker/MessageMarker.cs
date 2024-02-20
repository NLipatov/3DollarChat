using Ethachat.Server.Hubs.MessageDispatcher.Handlers.MessageSender;
using Microsoft.AspNetCore.SignalR;

namespace Ethachat.Server.Hubs.MessageDispatcher.Handlers.MessageMarker
{
    public class MessageMarker : IMessageMarker
    {

        public async Task MarkAsReaded(Guid messageId, string messageSender, IHubCallerClients clients)
        {
            await clients.Group(messageSender).SendAsync("MessageHasBeenRead", messageId);
        }

        public async Task MarkAsReceived(Guid messageId, string topicName, IHubCallerClients clients)
        {
            if (string.IsNullOrWhiteSpace(topicName))
                throw new ApplicationException("Cannot get an message sender username.");

            await clients.Group(topicName).SendAsync("OnReceiverMarkedMessageAsReceived", messageId);
        }
    }
}
