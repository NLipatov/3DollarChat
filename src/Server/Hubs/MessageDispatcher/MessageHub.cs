using Ethachat.Server.Hubs.MessageDispatcher.Handlers.MessageTransmitionGateway.Implementations;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.LongTermMessageStorage;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.SenderImplementations.
    EncryptedData;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.Implementation;
using Ethachat.Server.Hubs.UsersConnectedManaging.ConnectedUserStorage;
using Ethachat.Server.Hubs.UsersConnectedManaging.EventHandling;
using Ethachat.Server.Hubs.UsersConnectedManaging.EventHandling.OnlineUsersRequestEvent;
using Ethachat.Server.Services.Notifications.WebPush;
using Ethachat.Server.Utilities.HttpMessaging;
using Ethachat.Server.Utilities.UsernameResolver;
using EthachatShared.Models.Authentication.Models;
using EthachatShared.Models.Authentication.Models.Credentials.CredentialsDTO;
using EthachatShared.Models.ConnectedUsersManaging;
using EthachatShared.Models.Message;
using EthachatShared.Models.Message.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Ethachat.Server.Hubs.MessageDispatcher
{
    public class MessageHub : Hub
    {
        private readonly IServerHttpClient _serverHttpClient;
        private readonly IUserConnectedHandler<MessageHub> _userConnectedHandler;
        private readonly IOnlineUsersManager _onlineUsersManager;
        private readonly IWebPushNotificationService _webPushNotificationService;
        private readonly ILongTermStorageService<Message> _longTermStorageService;
        private readonly ILongTermStorageService<EncryptedDataTransfer> _longTermTransferStorageService;
        private readonly IUsernameResolverService _usernameResolverService;
        private static ReliableMessageSender _reliableMessageSender;
        private static IReliableMessageSender<EncryptedDataTransfer> _reliableTransferDataSender;
        private static IHubContext<MessageHub> _context;

        public MessageHub
        (IServerHttpClient serverHttpClient,
            IUserConnectedHandler<MessageHub> userConnectedHandler,
            IOnlineUsersManager onlineUsersManager,
            IWebPushNotificationService webPushNotificationService,
            ILongTermStorageService<Message> longTermStorageService,
            ILongTermStorageService<EncryptedDataTransfer> longTermTransferStorageService,
            IUsernameResolverService usernameResolverService,
            IHubContext<MessageHub> context)
        {
            _context = context;
            _serverHttpClient = serverHttpClient;
            _userConnectedHandler = userConnectedHandler;
            _onlineUsersManager = onlineUsersManager;
            _webPushNotificationService = webPushNotificationService;
            _longTermStorageService = longTermStorageService;
            _longTermTransferStorageService = longTermTransferStorageService;
            _usernameResolverService = usernameResolverService;

            if (_reliableMessageSender is null)
            {
                _reliableMessageSender =
                    new ReliableMessageSender(new SignalRGateway<Message>(_context), _longTermStorageService);
            }

            if (_reliableTransferDataSender is null)
            {
                _reliableTransferDataSender =
                    new EncryptedDataReliableSender(new SignalRGateway<EncryptedDataTransfer>(_context),
                        _longTermTransferStorageService);
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

            var storedTransferData = await _longTermTransferStorageService.GetSaved(usernameFromToken);
            foreach (var data in storedTransferData)
            {
                await TransferAsync(data);
            }

            var storedMessages = await _longTermStorageService.GetSaved(usernameFromToken);
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

            if (IsClientConnectedToHub(message.Target!))
                _reliableMessageSender.EnqueueAsync(message);
            else
            {
                _longTermStorageService.SaveAsync(message);
            }
        }

        public async Task TransferAsync(EncryptedDataTransfer dataTransfer)
        {
            await Clients.Caller.SendAsync("MessageRegisteredByHub", dataTransfer.Id);
            if (IsClientConnectedToHub(dataTransfer.Target))
                await _reliableTransferDataSender.EnqueueAsync(dataTransfer);
            else
            {
                _longTermTransferStorageService.SaveAsync(dataTransfer);
                SendNotificationAsync(dataTransfer);
            }

            await _context.Clients.Group(dataTransfer.Target).SendAsync("OnTransfer", dataTransfer);
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

        private async Task SendNotificationAsync<T>(T itemToNotifyAbout)
            where T : IHasInnerDataType, ISourceResolvable, IDestinationResolvable, IWebPushNotice
        {
            var isReceiverOnline = IsClientConnectedToHub(itemToNotifyAbout.Target);
            if (!isReceiverOnline)
            {
                await _webPushNotificationService.SendAsync(itemToNotifyAbout);
            }
        }

        public async Task OnTransferAcked(EncryptedDataTransfer edt)
        {
            _reliableTransferDataSender.OnAck(edt);
        }

        public async Task OnAck(Message syncMessage)
        {
            _reliableMessageSender.OnAck(syncMessage);
        }
    }
}