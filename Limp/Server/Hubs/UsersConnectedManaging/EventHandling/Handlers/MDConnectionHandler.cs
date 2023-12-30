using Ethachat.Client.Services.JWTReader;
using Limp.Server.Hubs.MessageDispatcher;
using Limp.Server.Hubs.UsersConnectedManaging.ConnectedUserStorage;
using Limp.Server.Utilities.HttpMessaging;
using Limp.Server.Utilities.UsernameResolver;
using LimpShared.Models.Authentication.Models;
using LimpShared.Models.Authentication.Models.Credentials.CredentialsDTO;
using LimpShared.Models.Authentication.Models.Credentials.Implementation;

namespace Limp.Server.Hubs.UsersConnectedManaging.EventHandling.Handlers
{
    public class MDConnectionHandler : IUserConnectedHandler<MessageHub>
    {
        private readonly IServerHttpClient _serverHttpClient;
        private readonly IUsernameResolverService _usernameResolverService;

        public MDConnectionHandler(IServerHttpClient serverHttpClient, IUsernameResolverService usernameResolverService)
        {
            _serverHttpClient = serverHttpClient;
            _usernameResolverService = usernameResolverService;
        }
        public void OnConnect(string connectionId)
        {
            if (!InMemoryHubConnectionStorage.MessageDispatcherHubConnections.Any(x => x.Value.Contains(connectionId)))
            {
                InMemoryHubConnectionStorage.MessageDispatcherHubConnections.TryAdd(connectionId, new List<string>() 
                { 
                    connectionId
                });
            }
        }

        public async void OnDisconnect
        (string connectionId, 
        Func<string, string, CancellationToken, Task>? RemoveUserFromGroup = null)
        {
            if (!InMemoryHubConnectionStorage.MessageDispatcherHubConnections.Any(x => x.Value.Contains(connectionId)))
                return;

            var targetConnection = InMemoryHubConnectionStorage.MessageDispatcherHubConnections
                .FirstOrDefault(x => x.Value.Contains(connectionId));

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
        Func<string, string, CancellationToken, Task>? AddUserToGroup,
        Func<string, string, CancellationToken, Task>? SendToCaller,
        Func<string, TokenRelatedOperationResult, CancellationToken, Task>? OnFaultTokenRelatedOperation,
        Func<string, Task>? CallUserHubMethodsOnUsernameResolved = null,
        WebAuthnPair? webAuthnPair = null,
        JwtPair? jwtPair = null)
        {
            GuaranteeDelegatesNotNull(new object?[] { AddUserToGroup, SendToCaller });
            bool isTokenValid = false;
            if (TokenReader.IsTokenReadable(jwtPair?.AccessToken ?? string.Empty))
            {
                var validationResult = await _serverHttpClient.ValidateCredentials(new CredentialsDTO(){JwtPair = jwtPair});
                isTokenValid = validationResult.Result is AuthResultType.Success;
            }
            else
            {
                if (webAuthnPair is not null)
                {
                    var validationResult = await _serverHttpClient.ValidateCredentials(new CredentialsDTO {WebAuthnPair =  webAuthnPair});
                    isTokenValid = validationResult.Result is AuthResultType.Success;
                }
            }
            if (!isTokenValid)
            {
                throw new ArgumentException("Access-token is not valid.");
            }

            var username = await _usernameResolverService.GetUsernameAsync(new CredentialsDTO {JwtPair = jwtPair, WebAuthnPair = webAuthnPair});

            //If there is a connection that has its connection id as a key, than its a unnamed connection.
            //we already have an proper username for this connection, so lets change a connection key
            if (InMemoryHubConnectionStorage.MessageDispatcherHubConnections.Any(x => x.Key == connectionId))
            {
                var oldConnections =
                    InMemoryHubConnectionStorage.MessageDispatcherHubConnections.Where(x => x.Key == connectionId);
                foreach (var connection in oldConnections)
                {
                    InMemoryHubConnectionStorage.MessageDispatcherHubConnections.TryAdd(username, connection.Value);
                    InMemoryHubConnectionStorage.MessageDispatcherHubConnections.TryRemove(connection);
                }
            }

            var userConnectionsIds =
                InMemoryHubConnectionStorage.MessageDispatcherHubConnections.Where(x => x.Key == username).SelectMany(x=>x.Value);
            foreach (var connection in userConnectionsIds)
            {
                await AddUserToGroup(connection, username, default);
            }

            await SendToCaller("OnMyNameResolve", username, default);
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
