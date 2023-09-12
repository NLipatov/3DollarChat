﻿using Limp.Client.Services.JWTReader;
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
            InMemoryHubConnectionStorage.MessageDispatcherHubConnections.TryAdd(Context.ConnectionId, new List<string> { Context.ConnectionId });

            await base.OnConnectedAsync();
        }

        public async override Task OnDisconnectedAsync(Exception? exception)
        {
            var keys = InMemoryHubConnectionStorage.MessageDispatcherHubConnections.Where(x => x.Value.Contains(Context.ConnectionId)).Select(x => x.Key);

            foreach (var key in keys)
            {
                var oldConnections = InMemoryHubConnectionStorage.MessageDispatcherHubConnections[key];
                var newConnections = oldConnections.Where(x => x != Context.ConnectionId).ToList();
                if (newConnections.Any())
                    InMemoryHubConnectionStorage.MessageDispatcherHubConnections.TryUpdate(key, newConnections, oldConnections);
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

        public async Task SetUsername(string accessToken)
        {
            string usernameFromToken = TokenReader.GetUsernameFromAccessToken(accessToken);

            await _userConnectedHandler.OnUsernameResolved
            (Context.ConnectionId, accessToken,
            Groups.AddToGroupAsync,
            Clients.Caller.SendAsync,
            Clients.Caller.SendAsync);

            var keys = InMemoryHubConnectionStorage.MessageDispatcherHubConnections.Where(x => x.Value.Contains(Context.ConnectionId)).Select(x => x.Key);

            foreach (var key in keys)
            {
                var connections = InMemoryHubConnectionStorage.MessageDispatcherHubConnections[key];
                InMemoryHubConnectionStorage.MessageDispatcherHubConnections.TryRemove(key, out _);
                InMemoryHubConnectionStorage.MessageDispatcherHubConnections.TryAdd(usernameFromToken, connections);

                await PushOnlineUsersToClients();
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
            if (string.IsNullOrWhiteSpace(message.Sender))
                throw new ArgumentException("Invalid message sender.");

            if (string.IsNullOrWhiteSpace(message.TargetGroup))
                throw new ArgumentException("Invalid target group of a message.");

            //message.Sender will be overriden somewhere here: await _messageSendHandler.SendAsync(message, Clients);
            if (message.Type == MessageType.UserMessage)
                await _webPushSender.SendPush($"You've got a new message from {message.Sender}", $"/user/{message.Sender}", message.TargetGroup);

            await _messageSendHandler.SendAsync(message, Clients);
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
            await _messageSendHandler.MarkAsReaded(messageId, messageSender, Clients);
        }

        public async Task SendWebPush(string username)
        {
            await _webPushSender.SendPush("Test web push", string.Empty, username);
        }
    }
}