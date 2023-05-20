using ClientServerCommon.Models.Message;
using Confluent.Kafka;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

namespace Limp.Server.Hubs.MessageDispatcher.Helpers
{
    public static class MessageSender
    {
        public static async Task SendAsync(Message message, IHubCallerClients clients)
        {
            if (string.IsNullOrWhiteSpace(message.TargetGroup))
                return;

            MessageStore.UnprocessedMessages.Add(message.Clone());

            //For personal chat we have Group with only one person in it
            //Send to members of this single-membered Group a message
            await clients.Group(message.TargetGroup).SendAsync("ReceiveMessage", message.ToReceiverRepresentation());

            await clients.Caller.SendAsync("ReceiveMessage", message.ToSenderRepresentation());
            //In the other case we need some message storage to be implemented to store a not delivered messages and remove them when they are delivered.
        }

        public static async Task AddAsUnprocessedAsync(Message message, IHubCallerClients clients)
        {
            //Wait 1 second and try again
            //Todo: send this message to undelivered message storage
            //  and deliver it when user is back online
            await Task.Delay(1000);
            await SendAsync(message, clients);
        }

        public static async Task OnMessageReceived(Guid messageId, IHubCallerClients clients)
        {
            Message? deliveredMessage = MessageStore.UnprocessedMessages.FirstOrDefault(x => x.Id == messageId);
            if (deliveredMessage != null && deliveredMessage.Sender != null)
            {
                MessageStore.UnprocessedMessages.Remove(deliveredMessage);
                await clients.Group(deliveredMessage.Sender!).SendAsync("MessageWasReceivedByRecepient", messageId);
            }
        }
    }
}
