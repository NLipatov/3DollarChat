using ClientServerCommon.Models.Message;
using System.Collections.ObjectModel;

namespace Limp.Client.Services.InboxService
{
    public interface IMessageBox
    {
        public List<Message> Messages { get; }
        Task MarkAsReceived(Guid messageId);
        Task AddMessageAsync(Message message, bool isEncrypted = true);
    }
}