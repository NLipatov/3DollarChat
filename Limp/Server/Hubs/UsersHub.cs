﻿using ClientServerCommon.Models;
using Limp.Client.Utilities;
using Limp.Server.Hubs.UsersConnectedManaging.ConnectedUserStorage;
using Limp.Server.Hubs.UsersConnectedManaging.EventHandling;
using Limp.Server.Hubs.UsersConnectedManaging.EventHandling.OnlineUsersRequestEvent;
using Limp.Server.Utilities.HttpMessaging;
using LimpShared.Encryption;
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
        public async override Task OnConnectedAsync() => 
            _userConnectedHandler.OnConnect(Context.ConnectionId);

        public async override Task OnDisconnectedAsync(Exception? exception) => 
            _userConnectedHandler.OnDisconnect(Context.ConnectionId, PushOnlineUsersToClients);

        public async Task SetUsername(string accessToken) => await _userConnectedHandler
            .OnUsernameResolved
            (Context.ConnectionId,
            accessToken,
            CallUserHubMethodsOnUsernameResolved: OnUsernameResolvedHandlers);

        public async Task SetRSAPublicKey(string accessToken, Key RSAPublicKey)
        {
            bool isTokenValid = await _serverHttpClient.IsAccessTokenValid(accessToken);

            string? username = isTokenValid ? TokenReader.GetUsername(accessToken) : null;

            if (isTokenValid && !string.IsNullOrWhiteSpace(username))
            {
                await PostAnRSAPublic(RSAPublicKey.Value.ToString(), username);
            }
            else
            {
                throw new ApplicationException("Cannot set an RSA Public key - given access token is not valid.");
            }
        }

        private async Task OnUsernameResolvedHandlers(string username)
        {
            await PushOnlineUsersToClients();
            await PushConId();
            await PushResolvedName(username);
        }

        private async Task PushResolvedName(string username)
        {
            await Clients.Caller.SendAsync("onNameResolve", username);
        }

        public async Task PushOnlineUsersToClients()
        {
            List<UserConnection> userConnections = _onlineUsersManager.GetOnlineUsers();
            await Clients.All.SendAsync("ReceiveOnlineUsers", userConnections);
        }

        public async Task PushConId()
        {
            await Clients.Caller.SendAsync("ReceiveConnectionId", Context.ConnectionId);
        }

        public async Task PostAnRSAPublic(string username, string PEMEncodedRSAPublicKey)
        {
            await _serverHttpClient.PostAnRSAPublic(username, PEMEncodedRSAPublicKey);
        }

        public async Task IsUserOnline(string username)
        {
            bool isUserConnectedToUsersHub =
                InMemoryHubConnectionStorage.UsersHubConnections.Any(x=>x.Username == username);

            bool isUserConnectedToMessageDispatcherHub =
                InMemoryHubConnectionStorage.MessageDispatcherHubConnections.Any(x => x.Username == username);

            bool isUserOnline = isUserConnectedToUsersHub && isUserConnectedToMessageDispatcherHub;

            await Clients.Caller.SendAsync("IsUserOnlineResponse", username, isUserOnline);
        }
    }
}