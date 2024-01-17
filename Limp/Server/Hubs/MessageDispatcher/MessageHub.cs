using Ethachat.Server.Hubs.MessageDispatcher.Helpers.MessageSender;
using Ethachat.Server.Hubs.UsersConnectedManaging.ConnectedUserStorage;
using Ethachat.Server.Hubs.UsersConnectedManaging.EventHandling;
using Ethachat.Server.Hubs.UsersConnectedManaging.EventHandling.OnlineUsersRequestEvent;
using Ethachat.Server.Utilities.HttpMessaging;
using Ethachat.Server.Utilities.Kafka;
using Ethachat.Server.Utilities.Redis.UnsentMessageHandling;
using Ethachat.Server.Utilities.UsernameResolver;
using Ethachat.Server.WebPushNotifications;
using EthachatShared.Models.Authentication.Models;
using EthachatShared.Models.Authentication.Models.Credentials.CredentialsDTO;
using EthachatShared.Models.ConnectedUsersManaging;
using EthachatShared.Models.Message;
using EthachatShared.Models.Message.DataTransfer;
using Microsoft.AspNetCore.SignalR;

namespace Ethachat.Server.Hubs.MessageDispatcher
{
    public class MessageHub : Hub
    {
        private readonly IServerHttpClient _serverHttpClient;
        private readonly IMessageBrokerService _messageBrokerService;
        private readonly IUserConnectedHandler<MessageHub> _userConnectedHandler;
        private readonly IOnlineUsersManager _onlineUsersManager;
        private readonly IMessageSendHandler _messageSendHandler;
        private readonly IWebPushSender _webPushSender;
        private readonly IUnsentMessagesRedisService _unsentMessagesRedisService;
        private readonly IUsernameResolverService _usernameResolverService;

        public MessageHub
        (IServerHttpClient serverHttpClient,
            IMessageBrokerService messageBrokerService,
            IUserConnectedHandler<MessageHub> userConnectedHandler,
            IOnlineUsersManager onlineUsersManager,
            IMessageSendHandler messageSender,
            IWebPushSender webPushSender,
            IUnsentMessagesRedisService unsentMessagesRedisService,
            IUsernameResolverService usernameResolverService)
        {
            _serverHttpClient = serverHttpClient;
            _messageBrokerService = messageBrokerService;
            _userConnectedHandler = userConnectedHandler;
            _onlineUsersManager = onlineUsersManager;
            _messageSendHandler = messageSender;
            _webPushSender = webPushSender;
            _unsentMessagesRedisService = unsentMessagesRedisService;
            _usernameResolverService = usernameResolverService;
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

            var storedMessages = await _unsentMessagesRedisService.GetSaved(usernameFromToken);
            foreach (var m in storedMessages.OrderBy(x => x.DateSent))
            {
                if (m.Package is not null)
                {
                    await DispatchData(m.Package, m.TargetGroup, m.Sender);
                }
                else
                {
                    await Dispatch(m);
                }
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

            if(IsClientConnectedToHub(message.TargetGroup!))
                await _messageSendHandler.SendAsync(message, Clients);
            else
                await _unsentMessagesRedisService.Save(message);

            await SendNotificationAsync(message);
        }

        private async Task SendNotificationAsync(Message message)
        {
            var isReceiverOnline = IsClientConnectedToHub(message.TargetGroup);
            if (!isReceiverOnline)
            {
                string contentDescription = message.Type switch
                {
                    MessageType.Metadata => "file",
                    _ => "message"
                };
                
                await _webPushSender.SendPush($"You've got a new {contentDescription} from {message.Sender}",
                    $"/user/{message.Sender}", message.TargetGroup);
            }
        }

        public async Task DispatchData(Package package, string receiver, string sender)
        {
            var packageMessage = new Message
            {
                Sender = sender,
                TargetGroup = receiver,
                Package = package,
                Type = MessageType.DataPackage
            };
            try
            {
                if (InMemoryHubConnectionStorage.MessageDispatcherHubConnections.Any(x => x.Key == receiver))
                {
                    await _messageSendHandler.SendAsync(packageMessage, Clients);
                }
                else
                {
                    await _unsentMessagesRedisService.Save(packageMessage);

                    if (package.Index == 0)
                        await _webPushSender.SendPush($"You've got a new file from {sender}", $"/user/{sender}",
                            receiver);
                }

                await Clients.Caller.SendAsync("PackageRegisteredByHub", package.FileDataid, package.Index);
            }
            catch (Exception e)
            {
                throw new ApplicationException(
                    $"{nameof(MessageHub)}.{nameof(DispatchData)}: could not dispatch a data: {e.Message}");
            }
        }

        public async Task DeleteConversation(string requester, string acceptor)
        {
            await Clients.Group(acceptor).SendAsync("OnConvertationDeleteRequest", requester);
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

        public async Task MessageReceived(Guid messageId, string topicName)
            => await _messageSendHandler.MarkAsReceived(messageId, topicName, Clients);

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
                await _messageSendHandler.MarkAsReaded(messageId, messageSender, Clients);
            }
        }
    }
}