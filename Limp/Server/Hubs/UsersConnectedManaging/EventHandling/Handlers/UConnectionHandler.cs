using Limp.Client.Services.JWTReader;
using Limp.Server.Hubs.UsersConnectedManaging.ConnectedUserStorage;
using LimpShared.Models.Authentication.Models;

namespace Limp.Server.Hubs.UsersConnectedManaging.EventHandling.Handlers
{
    public class UConnectionHandler : IUserConnectedHandler<UsersHub>
    {
        public void OnConnect(string connectionId)
        {
            if (!InMemoryHubConnectionStorage.UsersHubConnections.Any(x => x.Value.Contains(connectionId)))
            {
                bool added = InMemoryHubConnectionStorage.UsersHubConnections.TryAdd(Guid.NewGuid().ToString(), new List<string>() { connectionId });
                if (!added)
                    throw new ApplicationException($"Cannot add a user to a {nameof(InMemoryHubConnectionStorage.UsersHubConnections)} collection.");
            }
        }

        public void OnDisconnect
        (string connectionId,
        Func<string, string, CancellationToken, Task>? RemoveUserFromGroup = null)
        {
            string? existingUserUsername = InMemoryHubConnectionStorage.UsersHubConnections.FirstOrDefault(x => x.Value.Contains(connectionId)).Key;
            if (!string.IsNullOrWhiteSpace(existingUserUsername))
            {
                bool deleted = InMemoryHubConnectionStorage.UsersHubConnections.TryRemove(existingUserUsername, out _);
                if (!deleted)
                    throw new ApplicationException($"Cannot delete user from a {nameof(InMemoryHubConnectionStorage.UsersHubConnections)} collection.");
            }

            foreach (var connection in InMemoryHubConnectionStorage.UsersHubConnections.Where(x => x.Value.Count == 0))
            {
                InMemoryHubConnectionStorage.UsersHubConnections.TryRemove(connection);
            }
        }

        public async Task OnUsernameResolved
        (string connectionId,
        string accessToken,
        Func<string, string, CancellationToken, Task>? AddUserToGroup = null,
        Func<string, string, CancellationToken, Task>? callback = null,
        Func<string, TokenRelatedOperationResult, CancellationToken, Task>? OnFaultTokenRelatedOperation = null,
        Func<string, Task>? CallUserHubMethodsOnUsernameResolved = null)
        {
            string username = TokenReader.GetUsernameFromAccessToken(accessToken);
            var existingConnection = InMemoryHubConnectionStorage.UsersHubConnections.FirstOrDefault(x => x.Value.Contains(connectionId)).Key;

            if (existingConnection is not null)
            {
                bool added = InMemoryHubConnectionStorage.UsersHubConnections.TryAdd(username, InMemoryHubConnectionStorage.UsersHubConnections[existingConnection]);
                if (!added)
                    throw new ApplicationException($"Cannot add a user connection to {nameof(InMemoryHubConnectionStorage.UsersHubConnections)}.");

                bool deleted = InMemoryHubConnectionStorage.UsersHubConnections.TryRemove(existingConnection, out var _);
                if (!deleted)
                    throw new ApplicationException($"Cannot delete a user connection from {nameof(InMemoryHubConnectionStorage.UsersHubConnections)}.");
            }
            else
            {
                bool added = InMemoryHubConnectionStorage.UsersHubConnections.TryAdd(username, new List<string> { connectionId });
                if (!added)
                    throw new ApplicationException($"Cannot add a user connection to {nameof(InMemoryHubConnectionStorage.UsersHubConnections)}.");
            }

            if (CallUserHubMethodsOnUsernameResolved is not null)
                await CallUserHubMethodsOnUsernameResolved(username);
        }
    }
}
