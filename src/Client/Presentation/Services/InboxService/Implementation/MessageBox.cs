using Client.Transfer.Domain.TransferedEntities.Messages;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;
using EthachatShared.Models.Message;
using EthachatShared.Models.Message.ClientToClientTransferData;

namespace Ethachat.Client.Services.InboxService.Implementation
{
    public class MessageBox : IMessageBox
    {
        public MessageBox(ICallbackExecutor callbackExecutor)
        {
            _callbackExecutor = callbackExecutor;
        }

        private readonly ICallbackExecutor _callbackExecutor;
        private HashSet<Guid> _storedSet = new();

        public List<TextMessage> TextMessages { get; } = [];

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
                .Where(x => x.Target == targetGroup || x.Sender == targetGroup);

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

        public void AddMessage(TextMessage message)
        {
            if (_storedSet.Contains(message.Id))
            {
                Messages.First(x => x.Id == message.Id).AddChunk(new()
                {
                    Index = message.Index,
                    Total = message.Total,
                    Text = message.Text
                });

                _callbackExecutor.ExecuteSubscriptionsByName(message.Id, "TextMessageUpdate");
            }
            else
            {
                var clientMessage = new ClientMessage
                {
                    Id = message.Id,
                    Type = MessageType.TextMessage,
                    Sender = message.Sender,
                    DateReceived = DateTime.UtcNow,
                    Target = message.Target,
                };
                clientMessage.AddChunk(new TextChunk
                {
                    Text = message.Text,
                    Index = message.Index,
                    Total = message.Total
                });
                Messages.Add(clientMessage);

                _storedSet.Add(message.Id);

                _callbackExecutor.ExecuteSubscriptionsByName("MessageBoxUpdate");
            }
        }

        public void AddMessage(HlsPlaylistMessage playlistMessage)
        {
            AddMessage(new ClientMessage
            {
                Id = playlistMessage.Id,
                Sender = playlistMessage.Sender,
                Target = playlistMessage.Target,
                Type = MessageType.HLSPlaylist,
                HlsPlaylist = new HlsPlaylist
                {
                    M3U8Content = playlistMessage.Playlist,
                    Name = "video"
                }
            });
        }

        public void AddMessage(ClientMessage message)
        {
            if (Contains(message)) //duplicate
                return;

            Messages.Add(message);

            _storedSet.Add(GetMessageKey(message));

            _callbackExecutor.ExecuteSubscriptionsByName("MessageBoxUpdate");

            _callbackExecutor.ExecuteSubscriptionsByName(message.Sender, "NewUnreadedMessage");
        }

        public Task OnDelivered(Guid messageId)
        {
            Message? message = Messages.FirstOrDefault(x => x.Id == messageId);
            if (message != null)
            {
                message.IsDelivered = true;
            }

            return Task.CompletedTask;
        }

        public Task OnRegistered(Guid messageId)
        {
            Message? message = Messages.FirstOrDefault(x => x.Id == messageId);
            if (message != null)
                message.IsRegisteredByHub = true;
            return Task.CompletedTask;
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