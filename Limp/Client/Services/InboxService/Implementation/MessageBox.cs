using Limp.Client.HubInteraction.Handlers.MessageDecryption;
using Limp.Client.Services.HubServices.CommonServices.CallbackExecutor;
using Limp.Client.Services.UndeliveredMessagesStore;
using LimpShared.Models.Message;

namespace Limp.Client.Services.InboxService.Implementation
{
    public class MessageBox : IMessageBox
    {
        private readonly IMessageDecryptor _messageDecryptor;
        private readonly ICallbackExecutor _callbackExecutor;
        private readonly IUndeliveredMessagesRepository _undeliveredMessagesRepository;

        public List<Message> Messages { get; private set; } = new();
        public MessageBox
        (IMessageDecryptor messageDecryptor,
        ICallbackExecutor callbackExecutor,
        IUndeliveredMessagesRepository undeliveredMessagesRepository)
        {
            _messageDecryptor = messageDecryptor;
            _callbackExecutor = callbackExecutor;
            _undeliveredMessagesRepository = undeliveredMessagesRepository;
        }
        public async Task AddMessageAsync(Message message, bool isEncrypted = true)
        {
            if (isEncrypted)
                message = await _messageDecryptor.DecryptAsync(message);

            Messages.Add(message);

            _callbackExecutor.ExecuteSubscriptionsByName(message, "MessageBoxUpdate");
        }

        public async Task MarkAsReceived(Guid messageId)
        {
            Message? message = Messages.FirstOrDefault(x => x.Id == messageId);
            if (message != null)
                message.IsReceived = true;

            await _undeliveredMessagesRepository.DeleteAsync(messageId);
        }
    }
}
