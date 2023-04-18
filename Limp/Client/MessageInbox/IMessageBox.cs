using ClientServerCommon.Models.Message;

namespace Limp.Client.TopicStorage
{
    public interface IMessageBox
    {
        Task AddMessageAsync(Message message, bool isEncrypted = true);
        List<Message> FetchMessagesFromMessageBox(string topic);
        void PerformSubscribedCalls(Message message);
        Guid Subscribe(Action<Message> newMessageHandler);
        void Unsubscribe(Guid subscriptionId);
        void UnsubscribeAll();
    }
}