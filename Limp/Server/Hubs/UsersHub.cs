﻿using Limp.Client.Services.JWTReader;
using Limp.Server.Hubs.UsersConnectedManaging.ConnectedUserStorage;
using Limp.Server.Hubs.UsersConnectedManaging.EventHandling;
using Limp.Server.Hubs.UsersConnectedManaging.EventHandling.OnlineUsersRequestEvent;
using Limp.Server.Utilities.HttpMessaging;
using LimpShared.Encryption;
using LimpShared.Models.Authentication.Models.AuthenticatedUserRepresentation.PublicKey;
using LimpShared.Models.ConnectedUsersManaging;
using LimpShared.Models.Users;
using LimpShared.Models.WebPushNotification;
using Microsoft.AspNetCore.SignalR;

namespace Limp.Server.Hubs
{
    public class UsersHub : Hub
    {
        private readonly IServerHttpClient _serverHttpClient;
        private readonly IUserConnectedHandler<UsersHub> _userConnectedHandler;
        private readonly IOnlineUsersManager _onlineUsersManager;

        public UsersHub
        (IServerHttpClient serverHttpClient,
        IUserConnectedHandler<UsersHub> userConnectedHandler,
        IOnlineUsersManager onlineUsersManager)
        {
            _serverHttpClient = serverHttpClient;
            _userConnectedHandler = userConnectedHandler;
            _onlineUsersManager = onlineUsersManager;
        }
        public async override Task OnConnectedAsync()
        {
            lock (this)
            {
                InMemoryHubConnectionStorage.UserConnections.Add(new UserConnection
                {
                    Username = "Unnamed user",
                    ConnectionIds = new List<string>() { Context.ConnectionId }
                });
            }

            await PushOnlineUsersToClients();
        }

        public async override Task OnDisconnectedAsync(Exception? exception)
        {
            lock (this)
            {
                var userConnections = InMemoryHubConnectionStorage.UserConnections.Where(x => x.ConnectionIds.Contains(Context.ConnectionId));

                foreach (var userConnection in userConnections)
                {
                    userConnection.ConnectionIds.Remove(Context.ConnectionId);
                }
            }

            await PushOnlineUsersToClients();
        }

        public async Task SetUsername(string accessToken)
        {
            lock (this)
            {
                var userConnections = InMemoryHubConnectionStorage.UserConnections.Where(x => x.ConnectionIds.Contains(Context.ConnectionId));

                foreach (var userConnection in userConnections)
                {
                    userConnection.Username = TokenReader.GetUsernameFromAccessToken(accessToken);
                }
            }

            await PushOnlineUsersToClients();
        }

        public async Task SetRSAPublicKey(string accessToken, Key RSAPublicKey)
        {
            bool isTokenValid = await _serverHttpClient.IsAccessTokenValid(accessToken);

            string? username = isTokenValid ? TokenReader.GetUsernameFromAccessToken(accessToken) : null;

            if (isTokenValid && !string.IsNullOrWhiteSpace(username))
            {
                await PostAnRSAPublic(new PublicKeyDTO
                {
                    Key = RSAPublicKey.Value!.ToString(),
                    Username = username
                });
            }
            else
            {
                throw new ApplicationException("Cannot set an RSA Public key - given access token is not valid.");
            }

            lock (this)
            {
                var userConnections = InMemoryHubConnectionStorage.UserConnections.Where(x => x.ConnectionIds.Contains(Context.ConnectionId));

                foreach (var userConnection in userConnections)
                {
                    userConnection.RSAPublicKey = RSAPublicKey;
                }
            }

            await PushOnlineUsersToClients();
        }

        private async Task OnUsernameResolvedHandlers(string username)
        {
            await PushOnlineUsersToClients();
            await PushConId();
            await PushResolvedName(username);
        }

        private async Task PushResolvedName(string username)
        {
            await Clients.Caller.SendAsync("OnNameResolve", username);
        }

        public async Task PushOnlineUsersToClients()
        {
            //Defines a set of clients that are connected to both UsersHub and MessageDispatcherHub at the same time
            UserConnectionsReport userConnections = _onlineUsersManager.FormUsersOnlineMessage();
            //Pushes set of clients to all the clients
            await Clients.All.SendAsync("ReceiveOnlineUsers", userConnections);
        }

        public async Task PushConId()
        {
            await Clients.Caller.SendAsync("ReceiveConnectionId", Context.ConnectionId);
        }

        public async Task PostAnRSAPublic(PublicKeyDTO publicKeyDTO)
        {
            await _serverHttpClient.PostAnRSAPublic(publicKeyDTO);
        }

        public async Task IsUserOnline(string username)
        {
            string[] userHubConnections =
                InMemoryHubConnectionStorage.UsersHubConnections.Where(x => x.Key == username).SelectMany(x => x.Value).ToArray();

            string[] messageHubConnections =
                InMemoryHubConnectionStorage.MessageDispatcherHubConnections.Where(x => x.Key == username).SelectMany(x => x.Value).ToArray();

            bool isOnline = userHubConnections.Length > 0 && messageHubConnections.Length > 0;

            await Clients.Caller.SendAsync("IsUserOnlineResponse", new UserConnection
            {
                Username = username,
                ConnectionIds = isOnline ? messageHubConnections : new string[0],
            });
        }

        public async Task AddUserWebPushSubscription(NotificationSubscriptionDTO notificationSubscriptionDTO)
        {
            await _serverHttpClient.AddUserWebPushSubscribtion(notificationSubscriptionDTO);
            await Clients.Caller.SendAsync("WebPushSubscriptionSetChanged");
        }

        public async Task GetUserWebPushSubscriptions(string accessToken)
        {
            string username = TokenReader.GetUsernameFromAccessToken(accessToken);
            var userSubscriptions = await _serverHttpClient.GetUserWebPushSubscriptionsByAccessToken(username);
            await Clients.Caller.SendAsync("ReceiveWebPushSubscriptions", userSubscriptions);
        }

        public async Task RemoveUserWebPushSubscriptions(NotificationSubscriptionDTO[] notificationSubscriptionDTOs)
        {
            await _serverHttpClient.RemoveUserWebPushSubscriptions(notificationSubscriptionDTOs);
            await Clients.Caller.SendAsync("RemovedFromWebPushSubscriptions", notificationSubscriptionDTOs);
            await Clients.Caller.SendAsync("WebPushSubscriptionSetChanged");
        }

        public async Task CheckIfUserExist(string username)
        {
            IsUserExistDTO response = await _serverHttpClient.CheckIfUserExists(username);
            await Clients.Caller.SendAsync("UserExistanceResponse", response);
        }
    }
}