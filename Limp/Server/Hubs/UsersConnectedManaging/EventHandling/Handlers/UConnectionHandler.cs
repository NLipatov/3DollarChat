using ClientServerCommon.Models;
using Limp.Client.Services;
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
            if (!InMemoryHubConnectionStorage.UsersHubConnections.Any(x => x.Value.Contains(connectionId)))
            {
                InMemoryHubConnectionStorage.UsersHubConnections.TryAdd(connectionId, new List<string>() { connectionId});
            }
        }

        public void OnDisconnect(string connectionId, Func<Task> callback, Func<string, string, CancellationToken, Task>? RemoveUserFromGroup = null)
        {
            if (InMemoryHubConnectionStorage.UsersHubConnections.Any(x => x.Value.Contains(connectionId)))
            {
                var targetConnection = InMemoryHubConnectionStorage.UsersHubConnections
                    .First(x => x.Value.Contains(connectionId));

                targetConnection.Value.Remove(connectionId);
            }

            foreach (var connection in InMemoryHubConnectionStorage.UsersHubConnections.Where(x => x.Value.Count == 0))
            {
                InMemoryHubConnectionStorage.UsersHubConnections.TryRemove(connection);
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

            //If there is a connection that has its connection id as a key, than its a unnamed connection.
            //we already have an proper username for this connection, so lets change a connection key
            if(InMemoryHubConnectionStorage.UsersHubConnections.Any(x => x.Key == connectionId))
            {
                //setup a new item with all the old connections
                var connectionToBeDeleted = InMemoryHubConnectionStorage.UsersHubConnections.FirstOrDefault(x=>x.Key == connectionId);
                InMemoryHubConnectionStorage.UsersHubConnections.TryAdd(username, connectionToBeDeleted.Value);
                //remove the old item
                InMemoryHubConnectionStorage.UsersHubConnections.TryRemove(connectionToBeDeleted);
            }

            await CallUserHubMethodsOnUsernameResolved(username);
        }
    }
}
