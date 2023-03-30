using ClientServerCommon.Models;
using Limp.Server.Hubs.UsersConnectedManaging.ConnectedUserStorage;
using Limp.Server.Hubs.UsersConnectedManaging.EventHandling;
using Limp.Server.Utilities.HttpMessaging;
using LimpShared.Authentification;
using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

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
            lock (InMemoryHubConnectionStorage.MessageDispatcherHubConnections)
            {
                if (!InMemoryHubConnectionStorage.MessageDispatcherHubConnections.Any(x => x.ConnectionIds.Contains(connectionId)))
                {
                    InMemoryHubConnectionStorage.MessageDispatcherHubConnections.Add(
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
            lock (InMemoryHubConnectionStorage.MessageDispatcherHubConnections)
            {
                if (InMemoryHubConnectionStorage.MessageDispatcherHubConnections.Any(x => x.ConnectionIds.Contains(connectionId)))
                {
                    var connectionToBeDeleted = InMemoryHubConnectionStorage.MessageDispatcherHubConnections
                        .First(x => x.ConnectionIds.Contains(connectionId));

                    InMemoryHubConnectionStorage.MessageDispatcherHubConnections.Remove(connectionToBeDeleted);
                }

                InMemoryHubConnectionStorage.MessageDispatcherHubConnections.RemoveAll(x=>x.ConnectionIds.Count() == 0);
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

            lock(InMemoryHubConnectionStorage.MessageDispatcherHubConnections)
            {
                if (InMemoryHubConnectionStorage.MessageDispatcherHubConnections.Any(x => x.Username == username))
                {
                    InMemoryHubConnectionStorage.MessageDispatcherHubConnections
                        .First(x => x.Username == username).ConnectionIds.Add(connectionId);
                }
                else
                {
                    var targetConnection = InMemoryHubConnectionStorage
                    .MessageDispatcherHubConnections
                    .FirstOrDefault(x => x.ConnectionIds.Contains(connectionId));

                    if (targetConnection != null)
                    {
                        InMemoryHubConnectionStorage
                        .MessageDispatcherHubConnections
                        .First(x => x.ConnectionIds.Contains(connectionId))
                        .Username = username;
                    }
                    else
                    {
                        InMemoryHubConnectionStorage
                        .MessageDispatcherHubConnections
                        .Add(new ClientServerCommon.Models.UserConnection
                        {
                            Username = username,
                            ConnectionIds = new List<string>
                            {
                        connectionId
                            }
                        });
                    }
                }
            }

            foreach (var connection in InMemoryHubConnectionStorage.MessageDispatcherHubConnections
                .Where(x => !string.IsNullOrWhiteSpace(x.Username)))
            {
                foreach (var connectionIdentifier in connection.ConnectionIds)
                {
                    await AddUserToGroup(connectionIdentifier, username, default);
                }
            }

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
