﻿using ClientServerCommon.Models.Message;

namespace Limp.Client.TopicStorage
{
    public static class MessageBox
    {
        private static Dictionary<Guid, Action<Message>> subscriptions = new();
        private static List<Message> Messages = new();
        public static void AddMessage(Message message)
        {
            Messages.Add(message);

            PerformSubscribedCalls(message);
        }

        public static List<Message> FetchMessagesFromMessageBox(string topic)
        {
            List<Message> messages = Messages.Where(x => x.Topic == topic).ToList();

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
