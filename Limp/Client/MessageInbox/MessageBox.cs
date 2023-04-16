using ClientServerCommon.Models.Message;
using Limp.Client.HubInteraction.Handlers.MessageDecryption;

namespace Limp.Client.TopicStorage
{
    public class MessageBox : IMessageBox
    {
        private readonly IMessageDecryptor _messageDecryptor;
        private Dictionary<Guid, Action<Message>> subscriptions = new();
        private List<Message> Messages = new();
        public MessageBox(IMessageDecryptor messageDecryptor)
        {
            _messageDecryptor = messageDecryptor;
        }
        public async Task AddMessageAsync(Message message, bool isEncrypted = true)
        {
            if (isEncrypted)
                message = await _messageDecryptor.DecryptAsync(message);

            Messages.Add(message);

            PerformSubscribedCalls(message);
        }

        public List<Message> FetchMessagesFromMessageBox(string topic)
        {
            List<Message> messages = Messages.Where(x => x.Topic == topic).ToList();

            return messages;
        }

        public void PerformSubscribedCalls(Message message)
        {
            foreach (var subscription in subscriptions)
            {
                subscription.Value(message);
            }
        }

        public Guid Subsctibe(Action<Message> newMessageHandler)
        {
            Guid subscriptionId = Guid.NewGuid();
            subscriptions.Add(subscriptionId, newMessageHandler);
            return subscriptionId;
        }

        public void Unsubscribe(Guid subscriptionId)
        {
            subscriptions.Remove(subscriptionId);
        }

        public void UnsubscribeAll()
        {
            subscriptions.Clear();
        }
    }
}
