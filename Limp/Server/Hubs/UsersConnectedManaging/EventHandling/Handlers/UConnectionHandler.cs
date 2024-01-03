using Limp.Server.Hubs.UsersConnectedManaging.ConnectedUserStorage;
using Limp.Server.Utilities.UsernameResolver;
using EthachatShared.Models.Authentication.Models;
using EthachatShared.Models.Authentication.Models.Credentials.CredentialsDTO;
using EthachatShared.Models.Authentication.Models.Credentials.Implementation;

namespace Limp.Server.Hubs.UsersConnectedManaging.EventHandling.Handlers
{
    public class UConnectionHandler : IUserConnectedHandler<UsersHub>
    {
        private readonly IUsernameResolverService _usernameResolverService;

        public UConnectionHandler(IUsernameResolverService usernameResolverService)
        {
            _usernameResolverService = usernameResolverService;
        }
        
        public async void OnConnect(string connectionId)
        {
            if (!InMemoryHubConnectionStorage.UsersHubConnections.Any(x => x.Value.Contains(connectionId)))
            {
                InMemoryHubConnectionStorage.UsersHubConnections.TryAdd(connectionId, new List<string>() { connectionId });
            }
        }

        public void OnDisconnect
        (string connectionId,
        Func<string, string, CancellationToken, Task>? RemoveUserFromGroup = null)
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
        }

        public async Task OnUsernameResolved
        (string connectionId,
        string username,
        Func<string, string, CancellationToken, Task>? AddUserToGroup = null,
        Func<string, string, CancellationToken, Task>? callback = null,
        Func<string, TokenRelatedOperationResult, CancellationToken, Task>? OnFaultTokenRelatedOperation = null,
        Func<string, Task>? CallUserHubMethodsOnUsernameResolved = null,
        WebAuthnPair? webAuthnPair = null,
        JwtPair? jwtPair = null)
        {
            //If there is a connection that has its connection id as a key, than its a unnamed connection.
            //we already have an proper username for this connection, so lets change a connection key
            if (InMemoryHubConnectionStorage.UsersHubConnections.Any(x => x.Key == connectionId))
            {
                //setup a new item with all the old connections
                var connectionToBeDeleted = InMemoryHubConnectionStorage.UsersHubConnections.FirstOrDefault(x => x.Key == connectionId);
                InMemoryHubConnectionStorage.UsersHubConnections.TryAdd(username, connectionToBeDeleted.Value);
                //remove the old item
                InMemoryHubConnectionStorage.UsersHubConnections.TryRemove(connectionToBeDeleted);
            }

            await CallUserHubMethodsOnUsernameResolved(username);
        }
    }
}
