using ClientServerCommon.Models.Message;
using Confluent.Kafka;
using Limp.Server.Hubs.MessageDispatcher.Helpers.UndeliveredMessagesRegistry;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

namespace Limp.Server.Hubs.MessageDispatcher.Helpers.MessageSender
{
    public class MessageSender : IMessageSender
    {
        private readonly IUndeliveredMessagesStorer _undeliveredMessagesStorer;

        public MessageSender(IUndeliveredMessagesStorer undeliveredMessagesStorer)
        {
            _undeliveredMessagesStorer = undeliveredMessagesStorer;
        }
        public async Task SendAsync(Message message, IHubCallerClients clients)
        {
            if (string.IsNullOrWhiteSpace(message.TargetGroup))
                return;

            _undeliveredMessagesStorer.Add(message);

            //For personal chat we have Group with only one person in it
            //Send to members of this single-membered Group a message
            await clients.Group(message.TargetGroup).SendAsync("ReceiveMessage", message.ToReceiverRepresentation());

            await clients.Caller.SendAsync("ReceiveMessage", message.ToSenderRepresentation());
            //In the other case we need some message storage to be implemented to store a not delivered messages and remove them when they are delivered.
        }

        public async Task AddAsUnprocessedAsync(Message message, IHubCallerClients clients)
        {
            _undeliveredMessagesStorer.Add(message);
        }

        public async Task OnMessageReceived(Message message, IHubCallerClients clients)
        {
            _undeliveredMessagesStorer.Remove(message);
            await clients.Group(message.Sender!).SendAsync("MessageWasReceivedByRecepient", message.Id);
        }
    }
}
