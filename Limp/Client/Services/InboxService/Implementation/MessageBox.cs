using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;
using EthachatShared.Models.Message;

namespace Ethachat.Client.Services.InboxService.Implementation
{
    public class MessageBox : IMessageBox
    {
        public MessageBox
            (ICallbackExecutor callbackExecutor)
        {
            _callbackExecutor = callbackExecutor;
        }

        private readonly ICallbackExecutor _callbackExecutor;
        private HashSet<Guid> _storedSet = new();

        public List<ClientMessage> Messages { get; private set; } = new();

        public bool Contains(Message message) => _storedSet.Contains(GetMessageKey(message));

        private Guid GetMessageKey(Message message)
        {
            return message.Type switch
            {
                MessageType.Metadata => (message.Metadata ?? throw new ArgumentException("Invalid metadata"))
                    .DataFileId,
                MessageType.DataPackage => (message.Package ?? throw new ArgumentException("Invalid package"))
                    .FileDataid,
                _ => message.Id
            };
        }

        public void Delete(string targetGroup)
        {
            var messagesToRemove = Messages
                .Where(x => x.TargetGroup == targetGroup || x.Sender == targetGroup);

            Messages.RemoveAll(x => messagesToRemove.Contains(x));
            _storedSet.RemoveWhere(x => messagesToRemove.Any(m => GetMessageKey(m) == x));

            _callbackExecutor.ExecuteSubscriptionsByName("MessageBoxUpdate");
        }

        public void Delete(Message message)
        {
            Messages.RemoveAll(x => x.Id == message.Id);
            _storedSet.Remove(GetMessageKey(message));
            _callbackExecutor.ExecuteSubscriptionsByName("MessageBoxUpdate");
        }

        public void AddMessage(ClientMessage message)
        {
            if (message.SyncItem is not null)
                AddCompositeMessage(message);
            else 
                AddSingleMessage(message);
        }

        private void AddCompositeMessage(ClientMessage message)
        {
            if (message.SyncItem is null)
                throw new ArgumentException("Message is not composit");
            
            if (_storedSet.Contains(message.SyncItem.MessageId))
            {
                var compositeMessages = Messages.Where(x => x.SyncItem is not null && x.SyncItem.MessageId == message.SyncItem.MessageId).ToList();
                compositeMessages.Add(message);
                compositeMessages = compositeMessages
                    .DistinctBy(x=>x.SyncItem!.Index)
                    .OrderBy(x => x.SyncItem!.Index)
                    .ToList();
                
                var text = string.Join(string.Empty, compositeMessages.Select(x=>x.PlainText));
                message.Id = message.SyncItem.MessageId;
                Delete(message);
                message.PlainText = text;
                AddSingleMessage(message);
            }
            else
            {
                message.Id = message.SyncItem.MessageId;
                AddSingleMessage(message);
            }
        }

        private void AddSingleMessage(ClientMessage message)
        {
            Messages.Add(message);

            _storedSet.Add(GetMessageKey(message));

            _callbackExecutor.ExecuteSubscriptionsByName("MessageBoxUpdate");

            _callbackExecutor.ExecuteSubscriptionsByName(message.Sender, "NewUnreadedMessage");
        }

        public async Task OnDelivered(Guid messageId)
        {
            Message? message = Messages.FirstOrDefault(x => x.Id == messageId);
            if (message != null)
            {
                message.IsDelivered = true;
            }
        }

        public async Task OnRegistered(Guid messageId)
        {
            Message? message = Messages.FirstOrDefault(x => x.Id == messageId);
            if (message != null)
                message.IsRegisteredByHub = true;
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