using Limp.Client.ClientOnlyModels;

namespace Limp.Client.Services.InboxService
{
    public interface IMessageBox
    {
        public List<ClientMessage> Messages { get; }
        Task MarkAsReceived(Guid messageId);
        void MarkAsNotified(Guid messageId);
        Task AddMessageAsync(ClientMessage message, bool isEncrypted = true);
        Task AddMessagesAsync(ClientMessage[] messages, bool isEncrypted = true);
        public void MarkAsReaded(Guid messageId);
    }
}