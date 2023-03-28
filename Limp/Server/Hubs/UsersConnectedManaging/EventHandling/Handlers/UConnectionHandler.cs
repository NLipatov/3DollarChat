using ClientServerCommon.Models;
using Limp.Client.Utilities;
using Limp.Server.Hubs.UsersConnectedManaging.ConnectedUserStorage;
using Limp.Server.Utilities.HttpMessaging;
using LimpShared.Encryption;

namespace Limp.Server.Hubs.UsersConnectedManaging.EventHandling.Handlers
{
    public class UConnectionHandler : IUserConnectedHandler<UsersHub>
    {
        private readonly IServerHttpClient _serverHttpClient;
        public UConnectionHandler(IServerHttpClient serverHttpClient)
        {
            _serverHttpClient = serverHttpClient;
        }
        public async void OnConnect(string connectionId)
        {
            lock (InMemoryHubConnectionStorage.UsersHubConnections)
            {
                if (!InMemoryHubConnectionStorage.UsersHubConnections.Any(x => x.ConnectionIds.Contains(connectionId)))
                {
                    InMemoryHubConnectionStorage.UsersHubConnections.Add(
                    new UserConnection
                    {
                        ConnectionIds = new List<string>()
                        {
                            connectionId
                        }
                    });
                }
            }
        }

        public void OnDisconnect(string connectionId, Func<Task> callback)
        {
            lock (InMemoryHubConnectionStorage.UsersHubConnections)
            {
                if (InMemoryHubConnectionStorage.UsersHubConnections.Any(x => x.ConnectionIds.Contains(connectionId)))
                {
                    var connectionToBeDeleted = InMemoryHubConnectionStorage.UsersHubConnections
                        .First(x => x.ConnectionIds.Contains(connectionId));

                    InMemoryHubConnectionStorage.UsersHubConnections.Remove(connectionToBeDeleted);
                }

                InMemoryHubConnectionStorage.UsersHubConnections.RemoveAll(x => x.ConnectionIds.Count() == 0);
            }

            callback();
        }

        public async Task OnUsernameResolved
        (string connectionId, 
        string accessToken, 
        Func<string, string, CancellationToken, Task>? AddUserToGroup = null,
        Func<string, string, CancellationToken, Task>? callback = null,
        Func<string, Task>? CallUserHubMethodsOnUsernameResolved = null)
        {
            bool isTokenValid = await _serverHttpClient.IsAccessTokenValid(accessToken);

            var username = isTokenValid ? TokenReader.GetUsername(accessToken) : $"Anonymous_{Guid.NewGuid()}";

            Key publicKey = TokenReader.GetPublicKey(accessToken, username);

            if (InMemoryHubConnectionStorage.UsersHubConnections.Any(x => x.Username == username))
            {
                InMemoryHubConnectionStorage.UsersHubConnections
                    .First(x => x.Username == username).ConnectionIds.Add(connectionId);

                InMemoryHubConnectionStorage.UsersHubConnections
                    .First(x => x.Username == username).RSAPublicKey = publicKey;

                InMemoryHubConnectionStorage.UsersHubConnections.Remove
                    (InMemoryHubConnectionStorage
                    .UsersHubConnections
                    .First(x => x.ConnectionIds.Count == 1 && x.ConnectionIds.Contains(connectionId)));
            }
            else
            {
                InMemoryHubConnectionStorage
                    .UsersHubConnections
                    .First(x => x.ConnectionIds.Contains(connectionId)).Username = username;

                InMemoryHubConnectionStorage
                    .UsersHubConnections
                    .First(x => x.ConnectionIds.Contains(connectionId)).RSAPublicKey = publicKey;
            }

            await CallUserHubMethodsOnUsernameResolved(username);
        }
    }
}
