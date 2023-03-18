using ClientServerCommon.Models;
using Limp.Client.Utilities;
using Limp.Server.Hubs.UsersConnectedManaging.ConnectedUserStorage;
using Limp.Server.Hubs.UsersConnectedManaging.EventHandling;
using Limp.Server.Utilities.HttpMessaging;
using LimpShared.Encryption;
using Microsoft.AspNetCore.SignalR;

namespace Limp.Server.Hubs
{
    public class UsersHub : Hub
    {
        private readonly IServerHttpClient _serverHttpClient;
        private readonly IUserConnectedHandler<UsersHub> _userConnectedHandler;

        public UsersHub
        (IServerHttpClient serverHttpClient,
        IUserConnectedHandler<UsersHub> userConnectedHandler)
        {
            _serverHttpClient = serverHttpClient;
            _userConnectedHandler = userConnectedHandler;
        }
        public async override Task OnConnectedAsync() => _userConnectedHandler.OnConnect(Context.ConnectionId);

        public async override Task OnDisconnectedAsync(Exception? exception) => _userConnectedHandler.OnDisconnect(Context.ConnectionId, PushOnlineUsersToClients);
        
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
            await Clients.All.SendAsync("ReceiveOnlineUsers", InMemoryHubConnectionStorage.UsersHubConnections);
        }

        public async Task PushConId()
        {
            await Clients.Caller.SendAsync("ReceiveConnectionId", Context.ConnectionId);
        }

        public async Task PostAnRSAPublic(string username, string PEMEncodedRSAPublicKey)
        {
            await _serverHttpClient.PostAnRSAPublic(username, PEMEncodedRSAPublicKey);
        }
    }
}