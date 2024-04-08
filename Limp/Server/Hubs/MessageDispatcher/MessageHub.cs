using Ethachat.Server.Hubs.MessageDispatcher.Handlers.MessageMarker;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.MessageTransmitionGateway.Implementations;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.LongTermMessageStorage;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.Implementation;
using Ethachat.Server.Hubs.UsersConnectedManaging.ConnectedUserStorage;
using Ethachat.Server.Hubs.UsersConnectedManaging.EventHandling;
using Ethachat.Server.Hubs.UsersConnectedManaging.EventHandling.OnlineUsersRequestEvent;
using Ethachat.Server.Utilities.HttpMessaging;
using Ethachat.Server.Utilities.Kafka;
using Ethachat.Server.Utilities.UsernameResolver;
using Ethachat.Server.WebPushNotifications;
using EthachatShared.Models.Authentication.Models;
using EthachatShared.Models.Authentication.Models.Credentials.CredentialsDTO;
using EthachatShared.Models.ConnectedUsersManaging;
using EthachatShared.Models.Message;
using Microsoft.AspNetCore.SignalR;

namespace Ethachat.Server.Hubs.MessageDispatcher
{
    public class MessageHub : Hub
    {
        private readonly IServerHttpClient _serverHttpClient;
        private readonly IMessageBrokerService _messageBrokerService;
        private readonly IUserConnectedHandler<MessageHub> _userConnectedHandler;
        private readonly IOnlineUsersManager _onlineUsersManager;
        private readonly IMessageMarker _messageMarker;
        private readonly IWebPushSender _webPushSender;
        private readonly ILongTermMessageStorageService _longTermMessageStorageService;
        private readonly IUsernameResolverService _usernameResolverService;
        private static ReliableMessageSender _reliableMessageSender;
        private static IHubContext<MessageHub> _context;

        public MessageHub
        (IServerHttpClient serverHttpClient,
            IMessageBrokerService messageBrokerService,
            IUserConnectedHandler<MessageHub> userConnectedHandler,
            IOnlineUsersManager onlineUsersManager,
            IMessageMarker messageSender,
            IWebPushSender webPushSender,
            ILongTermMessageStorageService longTermMessageStorageService,
            IUsernameResolverService usernameResolverService,
            IHubContext<MessageHub> context)
        {
            _context = context;
            _serverHttpClient = serverHttpClient;
            _messageBrokerService = messageBrokerService;
            _userConnectedHandler = userConnectedHandler;
            _onlineUsersManager = onlineUsersManager;
            _messageMarker = messageSender;
            _webPushSender = webPushSender;
            _longTermMessageStorageService = longTermMessageStorageService;
            _usernameResolverService = usernameResolverService;

            if (_reliableMessageSender is null)
            {
                _reliableMessageSender = new ReliableMessageSender(new SignalRGateway(_context), _longTermMessageStorageService);
            }
        }

        public override async Task OnConnectedAsync()
        {
            InMemoryHubConnectionStorage.MessageDispatcherHubConnections.TryAdd(Context.ConnectionId,
                new List<string> { Context.ConnectionId });

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var keys = InMemoryHubConnectionStorage.MessageDispatcherHubConnections
                .Where(x => x.Value.Contains(Context.ConnectionId)).Select(x => x.Key);

            foreach (var key in keys)
            {
                var oldConnections = InMemoryHubConnectionStorage.MessageDispatcherHubConnections[key];
                var newConnections = oldConnections.Where(x => x != Context.ConnectionId).ToList();
                if (newConnections.Any())
                    InMemoryHubConnectionStorage.MessageDispatcherHubConnections.TryUpdate(key, newConnections,
                        oldConnections);
                else
                    InMemoryHubConnectionStorage.MessageDispatcherHubConnections.TryRemove(key, out _);

                await PushOnlineUsersToClients();
            }

            await base.OnDisconnectedAsync(exception);
        }

        private static bool IsClientConnectedToHub(string username)
        {
            lock (InMemoryHubConnectionStorage.MessageDispatcherHubConnections)
            {
                return InMemoryHubConnectionStorage.MessageDispatcherHubConnections.Any(x => x.Key == username);
            }
        }

        public async Task SetUsername(CredentialsDTO credentialsDto)
         {
            AuthResult usernameRequestResult = await _usernameResolverService.GetUsernameAsync(credentialsDto);
            if (usernameRequestResult.Result is not AuthResultType.Success)
            {
                await Clients.Caller.SendAsync("OnAccessTokenInvalid", usernameRequestResult);
            }

            var usernameFromToken = usernameRequestResult.Message ?? string.Empty;

            await _userConnectedHandler.OnUsernameResolved
            (Context.ConnectionId,
                usernameFromToken,
                Groups.AddToGroupAsync,
                Clients.Caller.SendAsync,
                Clients.Caller.SendAsync,
                webAuthnPair: credentialsDto.WebAuthnPair,
                jwtPair: credentialsDto.JwtPair);

            var keys = InMemoryHubConnectionStorage.MessageDispatcherHubConnections
                .Where(x => x.Value.Contains(Context.ConnectionId)).Select(x => x.Key);

            foreach (var key in keys)
            {
                var connections = InMemoryHubConnectionStorage.MessageDispatcherHubConnections[key];
                InMemoryHubConnectionStorage.MessageDispatcherHubConnections.TryRemove(key, out _);
                InMemoryHubConnectionStorage.MessageDispatcherHubConnections.TryAdd(usernameFromToken, connections);

                await PushOnlineUsersToClients();
            }

            var storedMessages = await _longTermMessageStorageService.GetSaved(usernameFromToken);
            foreach (var m in storedMessages.OrderBy(x => x.DateSent))
            {
                await Dispatch(m).ConfigureAwait(false);
            }
        }

        public async Task PushOnlineUsersToClients()
        {
            UserConnectionsReport report = _onlineUsersManager.FormUsersOnlineMessage();
            await Clients.All.SendAsync("ReceiveOnlineUsers", report);
        }

        /// <summary>
        /// Checks if target user is connected to the same hub.
        /// If so: sends him a message.
        /// If not: sends message to message broker.
        /// </summary>
        /// <param name="message">A message that needs to be send</param>
        /// <exception cref="ApplicationException"></exception>
        public async Task Dispatch(Message message)
        {
            SendRegistrationConfirmationAsync(message);

            if (IsClientConnectedToHub(message.TargetGroup!))
                _reliableMessageSender.EnqueueAsync(message);
            else
            {
                _longTermMessageStorageService.SaveAsync(message);
                if (message.Type is MessageType.Metadata or MessageType.TextMessage)
                    SendNotificationAsync(message);
            }
        }

        private async Task SendRegistrationConfirmationAsync(Message message)
        {
            if (message.Type is MessageType.Metadata)
            {
                await Clients.Group(message.Sender)
                    .SendAsync("MetadataRegisteredByHub", message.Metadata.DataFileId);
            }
            else if (message.Type is MessageType.Metadata)
            {
                await Clients.Group(message.Sender)
                    .SendAsync("MetadataRegisteredByHub", message.Metadata.DataFileId);
            }
            else if (message.Type is MessageType.DataPackage)
            {
                await Clients.Group(message.Sender!)
                    .SendAsync("PackageRegisteredByHub", message.Package!.FileDataid,
                        message.Package.Index);
            }
            else if (message.Type is MessageType.TextMessage)
            {
                await Clients.Caller.SendAsync("MessageRegisteredByHub", message.Id);
            }
        }

        private async Task SendNotificationAsync(Message message)
        {
            var isReceiverOnline = IsClientConnectedToHub(message.TargetGroup!);
            if (!isReceiverOnline)
            {
                string contentDescription = message.Type switch
                {
                    MessageType.Metadata => "file",
                    _ => "message"
                };
                
                await _webPushSender.SendPush($"You've got a new {contentDescription} from {message.Sender}",
                    $"/{message.Sender}", message.TargetGroup);
            }
        }

        public async Task DeleteConversation(string requester, string acceptor)
        {
            _ = _longTermMessageStorageService.GetSaved(acceptor);
            await Clients.Group(acceptor).SendAsync("OnConversationDeleteRequest", requester);
        }

        public async Task OnDataTranferSuccess(Guid fileId, string fileSender)
        {
            try
            {
                if (InMemoryHubConnectionStorage.MessageDispatcherHubConnections.Any(x => x.Key == fileSender))
                {
                    await Clients.Group(fileSender).SendAsync("OnFileTransfered", fileId);
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(
                    $"{nameof(MessageHub)}.{nameof(OnDataTranferSuccess)}: could not dispatch a data: {e.Message}");
            }
        }

        public async Task OnAck(Message syncMessage)
        {
            _reliableMessageSender.OnAck(syncMessage);
            
            await _messageMarker.MarkAsReceived(syncMessage.SyncItem.MessageId, syncMessage.Sender!, Clients);
        }

        /// <summary>
        /// Sends message to a message broker system
        /// </summary>
        /// <param name="message">Message to ship</param>
        public async Task Ship(Message message)
        {
            await _messageBrokerService.ProduceAsync(message);
        }

        public async Task GetAnRSAPublic(string username)
        {
            string? pubKey = await _serverHttpClient.GetAnRSAPublicKey(username);
            await Clients.Caller.SendAsync("ReceivePublicKey", username, pubKey);
        }

        public async Task MessageHasBeenRead(Guid messageId, string messageSender)
        {
            if (InMemoryHubConnectionStorage.MessageDispatcherHubConnections.Any(x => x.Key == messageSender))
            {
                await _messageMarker.MarkAsReaded(messageId, messageSender, Clients);
            }
        }

        public async Task OnTyping(string sender, string receiver)
        {
            await Clients.Group(receiver).SendAsync("OnTyping", sender);
        }
    }
}