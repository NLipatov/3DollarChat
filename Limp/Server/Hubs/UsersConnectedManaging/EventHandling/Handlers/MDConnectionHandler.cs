﻿using Limp.Client.Services.JWTReader;
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

            var username = await _usernameResolverService.GetUsernameAsync(jwtPair?.AccessToken ?? webAuthnPair?.CredentialId);

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

            await SendToCaller("OnMyNameResolve", username, default);
        }

        private async Task<TokenRelatedOperationResult> GetUsername(string accessToken)
        {
            TokenRelatedOperationResult usernameRequestResult = await _serverHttpClient.GetUserNameFromAccessTokenAsync(accessToken);

            string declaredUsername =  await _usernameResolverService.GetUsernameAsync(accessToken);
            string? actualUsername = usernameRequestResult.Username;

            if(!string.IsNullOrWhiteSpace(actualUsername)
                &&
                !string.IsNullOrWhiteSpace(declaredUsername))
            {
                if (!declaredUsername.Equals(actualUsername))
                    throw new ArgumentException("Username from access-token and username from AuthAPI differs.");
            }

            return usernameRequestResult;
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
