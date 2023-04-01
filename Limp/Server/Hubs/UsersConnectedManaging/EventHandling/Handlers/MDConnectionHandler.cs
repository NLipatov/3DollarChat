using Limp.Server.Hubs.UsersConnectedManaging.ConnectedUserStorage;
using Limp.Server.Utilities.HttpMessaging;
using LimpShared.Authentification;

namespace Limp.Server.Hubs.UsersConnectedManaging.EventHandling.Handlers
{
    public class MDConnectionHandler : IUserConnectedHandler<MessageDispatcherHub>
    {
        private readonly IServerHttpClient _serverHttpClient;

        public MDConnectionHandler(IServerHttpClient serverHttpClient)
        {
            _serverHttpClient = serverHttpClient;
        }
        public void OnConnect(string connectionId)
        {
            if (!InMemoryHubConnectionStorage.MessageDispatcherHubConnections.Any(x => x.Value.Contains(connectionId)))
            {
                InMemoryHubConnectionStorage.MessageDispatcherHubConnections.TryAdd(connectionId, new List<string>() { connectionId });
            }
        }

        public async void OnDisconnect(string connectionId, Func<string, string, CancellationToken, Task>? RemoveUserFromGroup = null)
        {
            var targetConnection = InMemoryHubConnectionStorage.MessageDispatcherHubConnections
                .First(x => x.Value.Contains(connectionId));

            await RemoveUserFromGroup(connectionId, targetConnection.Key, default);

            if (InMemoryHubConnectionStorage.MessageDispatcherHubConnections.Any(x => x.Value.Contains(connectionId)))
            {
                targetConnection.Value.Remove(connectionId);
            }

            foreach (var connection in InMemoryHubConnectionStorage.MessageDispatcherHubConnections.Where(x => x.Value.Count == 0))
            {
                InMemoryHubConnectionStorage.MessageDispatcherHubConnections.TryRemove(connection);
            }
        }

        public async Task OnUsernameResolved
        (string connectionId,
        string accessToken,
        Func<string, string, CancellationToken, Task>? AddUserToGroup,
        Func<string, string, CancellationToken, Task>? callback,
        Func<string, Task>? CallUserHubMethodsOnUsernameResolved = null)
        {
            GuaranteeDelegatesNotNull(new object?[] { AddUserToGroup, callback });

            string username = await GetUsername(accessToken);

            //If there is a connection that has its connection id as a key, than its a unnamed connection.
            //we already have an proper username for this connection, so lets change a connection key
            if (InMemoryHubConnectionStorage.MessageDispatcherHubConnections.Any(x => x.Key == connectionId))
            {
                //setup a new item with all the old connections
                var connectionToBeDeleted = InMemoryHubConnectionStorage.MessageDispatcherHubConnections.FirstOrDefault(x => x.Key == connectionId);
                InMemoryHubConnectionStorage.MessageDispatcherHubConnections.TryAdd(username, connectionToBeDeleted.Value);
                //remove the old item
                InMemoryHubConnectionStorage.MessageDispatcherHubConnections.TryRemove(connectionToBeDeleted);
            }

            await AddUserToGroup(connectionId, username, default);

            await callback("OnMyNameResolve", username, default);
        }

        private async Task<string> GetUsername(string accessToken)
        {
            TokenRelatedOperationResult usernameRequest = await _serverHttpClient.GetUserNameFromAccessTokenAsync(accessToken);

            string? username = usernameRequest.Username;

            if (string.IsNullOrWhiteSpace(username))
                throw new ApplicationException($"Could not resolve a username by given user JWT token.");

            return username;
        }

        private void GuaranteeDelegatesNotNull(params object?[] delegateObjects)
        {
            foreach (var delegateObject in delegateObjects)
            {
                if (delegateObject == null)
                    throw new ArgumentException
                        ($"Value of {nameof(delegateObject)} was null." +
                        $" This event handler requires non-null delegate to perform event handling.");
            }
        }
    }
}
