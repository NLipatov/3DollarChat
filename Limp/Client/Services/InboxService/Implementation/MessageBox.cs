using Limp.Client.ClientOnlyModels;
using Limp.Client.ClientOnlyModels.ClientOnlyExtentions;
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

        public List<ClientMessage> Messages { get; private set; } = new();
        public MessageBox
        (IMessageDecryptor messageDecryptor,
        ICallbackExecutor callbackExecutor,
        IUndeliveredMessagesRepository undeliveredMessagesRepository)
        {
            _messageDecryptor = messageDecryptor;
            _callbackExecutor = callbackExecutor;
            _undeliveredMessagesRepository = undeliveredMessagesRepository;

            _ = AddUndeliveredMessagesFromLocalStorageAsync();
        }
        private async Task AddUndeliveredMessagesFromLocalStorageAsync()
        {
            var undeliveredMessages = await _undeliveredMessagesRepository.GetUndeliveredAsync();
            Messages.AddRange(undeliveredMessages);
            _callbackExecutor.ExecuteSubscriptionsByName("MessageBoxUpdate");
        }
        public async Task AddMessageAsync(ClientMessage message, bool isEncrypted = true)
        {
            if (isEncrypted)
                message.PlainText = await _messageDecryptor.DecryptAsync(message);

            Messages.Add(message);

            _callbackExecutor.ExecuteSubscriptionsByName("MessageBoxUpdate");
        }

        public async Task AddMessagesAsync(ClientMessage[] messages, bool isEncrypted = true)
        {
            if (isEncrypted)
            {
                foreach (var encryptedMessage in messages)
                {
                    encryptedMessage.PlainText = await _messageDecryptor.DecryptAsync(encryptedMessage);
                }
            }

            Messages.AddRange(messages);

            _callbackExecutor.ExecuteSubscriptionsByName("MessageBoxUpdate");
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
