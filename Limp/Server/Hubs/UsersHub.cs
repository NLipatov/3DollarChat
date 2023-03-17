using ClientServerCommon.Models;
using Limp.Client.Utilities;
using Limp.Server.Hubs.UserStorage;
using Limp.Server.Utilities.HttpMessaging;
using LimpShared.Encryption;
using Microsoft.AspNetCore.SignalR;

namespace Limp.Server.Hubs
{
    public class UsersHub : Hub
    {
        private readonly IServerHttpClient _serverHttpClient;
        public UsersHub(IServerHttpClient serverHttpClient)
        {
            _serverHttpClient = serverHttpClient;
        }
        public async override Task OnConnectedAsync()
        {
            lock(InMemoryUsersStorage.UsersHubConnections)
            {
                if (!InMemoryUsersStorage.UsersHubConnections.Any(x => x.ConnectionIds.Contains(Context.ConnectionId)))
                {
                    InMemoryUsersStorage.UsersHubConnections.Add(new UserConnections { ConnectionIds = new List<string>() { Context.ConnectionId } });
                }
            }
        }

        public async override Task OnDisconnectedAsync(Exception? exception)
        {
            lock(InMemoryUsersStorage.UsersHubConnections)
            {
                var targetConnectionId = Context.ConnectionId;
                InMemoryUsersStorage.UsersHubConnections.First(x => x.ConnectionIds.Contains(targetConnectionId)).ConnectionIds.Remove(targetConnectionId);

                InMemoryUsersStorage.UsersHubConnections.RemoveAll(x => x.ConnectionIds.Count == 0);
            }

            await PushOnlineUsersToClients();
        }

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

        public async Task SetUsername(string accessToken)
        {
            bool isTokenValid = await _serverHttpClient.IsAccessTokenValid(accessToken);

            var username = isTokenValid ? TokenReader.GetUsername(accessToken) : $"Anonymous_{Guid.NewGuid()}";

            Key publicKey = TokenReader.GetPublicKey(accessToken, username);

            if (InMemoryUsersStorage.UsersHubConnections.Any(x => x.Username == username))
            {
                InMemoryUsersStorage.UsersHubConnections.First(x => x.Username == username).ConnectionIds.Add(Context.ConnectionId);
                InMemoryUsersStorage.UsersHubConnections.First(x => x.Username == username).RSAPublicKey = publicKey;

                InMemoryUsersStorage.UsersHubConnections.Remove
                    (InMemoryUsersStorage
                    .UsersHubConnections
                    .First(x => x.ConnectionIds.Count == 1 && x.ConnectionIds.Contains(Context.ConnectionId)));
            }
            else
            {
                InMemoryUsersStorage
                    .UsersHubConnections
                    .First(x => x.ConnectionIds.Contains(Context.ConnectionId)).Username = username;

                InMemoryUsersStorage
                    .UsersHubConnections
                    .First(x => x.ConnectionIds.Contains(Context.ConnectionId)).RSAPublicKey = publicKey;
            }

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
            await Clients.All.SendAsync("ReceiveOnlineUsers", InMemoryUsersStorage.UsersHubConnections);
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