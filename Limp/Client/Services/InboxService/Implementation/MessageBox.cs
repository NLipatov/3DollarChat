using Limp.Client.ClientOnlyModels;
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
                message.PlainText = (await _messageDecryptor.DecryptAsync(message)).Cyphertext;

            Messages.Add(message);

            _callbackExecutor.ExecuteSubscriptionsByName("MessageBoxUpdate");

            _callbackExecutor.ExecuteSubscriptionsByName(message.Sender, "NewUnreadedMessage");
        }

        public async Task AddMessagesAsync(ClientMessage[] messages, bool isEncrypted = true)
        {
            if (isEncrypted)
            {
                foreach (var encryptedMessage in messages)
                {
                    encryptedMessage.PlainText = (await _messageDecryptor.DecryptAsync(encryptedMessage)).Cyphertext;
                }
            }

            Messages.AddRange(messages);

            _callbackExecutor.ExecuteSubscriptionsByName("MessageBoxUpdate");
        }

        public async Task OnDelivered(Guid messageId)
        {
            Message? message = Messages.FirstOrDefault(x => x.Id == messageId);
            if (message != null)
                message.IsDelivered = true;

            await _undeliveredMessagesRepository.DeleteAsync(messageId);
        }

        public async Task OnRegistered(Guid messageId)
        {
            Message? message = Messages.FirstOrDefault(x => x.Id == messageId);
            if (message != null)
                message.IsRegisteredByHub = true;

            await _undeliveredMessagesRepository.DeleteAsync(messageId);
        }

        public void OnToastWasShown(Guid messageId)
            => Messages.First(x => x.Id == messageId).IsToastShown = true;

        public void OnSeen(Guid messageId)
        {
            ClientMessage? message = Messages.FirstOrDefault(x => x.Id == messageId);
            if (message is not null)
            {
                message.IsSeen = true;
            }
        }
    }
}
