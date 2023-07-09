using ClientServerCommon.Models;
using Limp.Server.Hubs.MessageDispatcher.Helpers.MessageSender;
using Limp.Server.Hubs.UsersConnectedManaging.ConnectedUserStorage;
using Limp.Server.Hubs.UsersConnectedManaging.EventHandling;
using Limp.Server.Hubs.UsersConnectedManaging.EventHandling.OnlineUsersRequestEvent;
using Limp.Server.Utilities.HttpMessaging;
using Limp.Server.Utilities.Kafka;
using Limp.Server.WebPushNotifications;
using LimpShared.Models.ConnectedUsersManaging;
using LimpShared.Models.Message;
using Microsoft.AspNetCore.SignalR;
using WebPush;

namespace Limp.Server.Hubs.MessageDispatcher
{
    public class MessageHub : Hub
    {
        private readonly IServerHttpClient _serverHttpClient;
        private readonly IMessageBrokerService _messageBrokerService;
        private readonly IUserConnectedHandler<MessageHub> _userConnectedHandler;
        private readonly IOnlineUsersManager _onlineUsersManager;
        private readonly IMessageSendHandler _messageSendHandler;
        private readonly IWebPushSender _webPushSender;

        public MessageHub
        (IServerHttpClient serverHttpClient,
        IMessageBrokerService messageBrokerService,
        IUserConnectedHandler<MessageHub> userConnectedHandler,
        IOnlineUsersManager onlineUsersManager,
        IMessageSendHandler messageSender,
        IWebPushSender webPushSender)
        {
            _serverHttpClient = serverHttpClient;
            _messageBrokerService = messageBrokerService;
            _userConnectedHandler = userConnectedHandler;
            _onlineUsersManager = onlineUsersManager;
            _messageSendHandler = messageSender;
            _webPushSender = webPushSender;
        }

        public async override Task OnConnectedAsync()
        {
            _userConnectedHandler.OnConnect(Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public async override Task OnDisconnectedAsync(Exception? exception)
        {
            _userConnectedHandler.OnDisconnect(Context.ConnectionId, RemoveUserFromGroup: Groups.RemoveFromGroupAsync);
            await base.OnDisconnectedAsync(exception);
        }

        private static bool IsClientConnectedToHub(string username)
        {
            lock (InMemoryHubConnectionStorage.MessageDispatcherHubConnections)
            {
                return InMemoryHubConnectionStorage.MessageDispatcherHubConnections.Any(x => x.Key == username);
            }
        }

        public async Task SetUsername(string accessToken)
        {
            await _userConnectedHandler.OnUsernameResolved
            (Context.ConnectionId, accessToken,
            Groups.AddToGroupAsync,
            Clients.Caller.SendAsync,
            Clients.Caller.SendAsync);
            await PushOnlineUsersToClients();
        }

        public async Task PushOnlineUsersToClients()
        {
            UserConnectionsReport userConnections = _onlineUsersManager.FormUsersOnlineMessage();
            await Clients.All.SendAsync("ReceiveOnlineUsers", userConnections);
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
            if (string.IsNullOrWhiteSpace(message.TargetGroup))
                throw new ArgumentException("Invalid target group of a message.");

            await _messageSendHandler.SendAsync(message, Clients);

            if(message.Type == MessageType.UserMessage)
                await _webPushSender.SendPush($"You've got a new message from {message.Sender}", $"https://google.com", message.TargetGroup);
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
    }
}