using ClientServerCommon.Models.Message;
using Limp.Client.HubInteraction.Handlers.MessageDecryption;
using Limp.Client.Services.HubServices.CommonServices.CallbackExecutor;

namespace Limp.Client.Services.InboxService.Implementation
{
    public class MessageBox : IMessageBox
    {
        private readonly IMessageDecryptor _messageDecryptor;
        private readonly ICallbackExecutor _callbackExecutor;
        public List<Message> Messages { get; private set; } = new();
        public MessageBox
        (IMessageDecryptor messageDecryptor,
        ICallbackExecutor callbackExecutor)
        {
            _messageDecryptor = messageDecryptor;
            _callbackExecutor = callbackExecutor;
        }
        public async Task AddMessageAsync(Message message, bool isEncrypted = true)
        {
            if (isEncrypted)
                message = await _messageDecryptor.DecryptAsync(message);

            Messages.Add(message);

            _callbackExecutor.ExecuteSubscriptionsByName(message, "MessageBoxUpdate");
        }

        public void MarkAsReceived(Guid messageId)
        {
            Message? message = Messages.FirstOrDefault(x => x.Id == messageId);
            if (message != null)
                message.IsReceived = true;
        }
    }
}
