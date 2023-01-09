using Limp.Shared.Models;

namespace Limp.Client.TopicStorage
{
    public static class MessageBox
    {
        private static Dictionary<Guid, Action<Message>> subscriptions = new Dictionary<Guid, Action<Message>>();
        private static List<Message> Messages = new List<Message>();
        public static void AddMessage(Message message)
        {
            Messages.Add(message);

            PerformSubscribedCalls(message);
        }

        public static List<Message> FetchMessagesFromMessageBox(string sender)
        {
            List<Message> messages = Messages.Where(x=>x.Sender == sender).ToList();

            return messages;
        }

        public static void PerformSubscribedCalls(Message message)
        {
            foreach (var subscription in subscriptions)
            {
                subscription.Value(message);
            }
        }

        public static Guid Subsctibe(Action<Message> newMessageHandler)
        {
            Guid subscriptionId = Guid.NewGuid();
            subscriptions.Add(subscriptionId, newMessageHandler);
            return subscriptionId;
        }

        public static void Unsubscribe(Guid subscriptionId)
        {
            subscriptions.Remove(subscriptionId);
        }

        public static void UnsubscribeAll()
        {
            subscriptions.Clear();
        }
    }
}
