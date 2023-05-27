using ClientServerCommon.Models.Message;

namespace Limp.Client.Services.InboxService
{
    public interface IMessageBox
    {
        void MarkAsReceived(Guid messageId);
        Task AddMessageAsync(Message message, bool isEncrypted = true);
        List<Message> FetchMessagesFromMessageBox(string topic);
    }
}