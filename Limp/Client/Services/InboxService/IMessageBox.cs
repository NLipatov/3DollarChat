using Limp.Client.ClientOnlyModels;
using LimpShared.Models.Message;
using System.Collections.ObjectModel;

namespace Limp.Client.Services.InboxService
{
    public interface IMessageBox
    {
        public List<ClientMessage> Messages { get; }
        Task MarkAsReceived(Guid messageId);
        void MarkAsNotified(Guid messageId);
        void MarkAsRead(Guid messageId);
        Task AddMessageAsync(ClientMessage message, bool isEncrypted = true);
        Task AddMessagesAsync(ClientMessage[] messages, bool isEncrypted = true);
        Task<ClientMessage[]> GetUnreadedMessages(string partnerName);
    }
}