using System.Collections.Concurrent;
using Client.Application.Gateway;
using Client.Infrastructure.Gateway;
using Ethachat.Client.Services.AuthenticationService.Handlers;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;
using EthachatShared.Constants;
using EthachatShared.Models.Authentication.Models.Credentials.CredentialsDTO;
using EthachatShared.Models.ConnectedUsersManaging;
using EthachatShared.Models.Users;
using EthachatShared.Models.WebPushNotification;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.UsersService.Implementation
{
    public class UsersService : IUsersService
    {
        public NavigationManager NavigationManager { get; set; }
        private readonly ICallbackExecutor _callbackExecutor;
        private readonly IAuthenticationHandler _authenticationHandler;
        private bool _isConnectionClosedCallbackSet = false;

        private ConcurrentDictionary<Guid, Func<string, Task>> ConnectionIdReceivedCallbacks = new();
        private ConcurrentDictionary<Guid, Func<string, Task>> UsernameResolvedCallbacks = new();
        private IGateway? _gateway;

        private async Task<IGateway> ConfigureGateway()
        {
            var gateway = new SignalRGateway();
            await gateway.ConfigureAsync(NavigationManager.ToAbsoluteUri(HubAddress.Users),
                async () => await _authenticationHandler.GetCredentialsDto());
            return gateway;
        }

        public UsersService
        (NavigationManager navigationManager,
            ICallbackExecutor callbackExecutor,
            IAuthenticationHandler authenticationHandler)
        {
            NavigationManager = navigationManager;
            _callbackExecutor = callbackExecutor;
            _authenticationHandler = authenticationHandler;
        }


        public async Task<HubConnection> GetHubConnectionAsync()
        {
            if (!await _authenticationHandler.IsSetToUseAsync())
            {
                NavigationManager.NavigateTo("signin");
            }

            await InitializeGatewayAsync();
            return null;
        }

        private async Task InitializeGatewayAsync()
        {
            _gateway ??= await ConfigureGateway();

            await _gateway.AddEventCallbackAsync<UserConnectionsReport>("ReceiveOnlineUsers",
                updatedTrackedUserConnections =>
                {
                    _callbackExecutor.ExecuteSubscriptionsByName(updatedTrackedUserConnections, "ReceiveOnlineUsers");
                    return Task.CompletedTask;
                });

            await _gateway.AddEventCallbackAsync<string>("ReceiveConnectionId",
                connectionId =>
                {
                    _callbackExecutor.ExecuteCallbackDictionary(connectionId, ConnectionIdReceivedCallbacks);
                    return Task.CompletedTask;
                });

            await _gateway.AddEventCallbackAsync<string>("OnNameResolve", async username =>
            {
                _callbackExecutor.ExecuteSubscriptionsByName(username, "OnNameResolve");

                _callbackExecutor.ExecuteCallbackDictionary(username, UsernameResolvedCallbacks);
            });

            await _gateway.AddEventCallbackAsync<UserConnection>("IsUserOnlineResponse",
                (UserConnection) =>
                {
                    _callbackExecutor.ExecuteSubscriptionsByName(UserConnection, "IsUserOnlineResponse");
                    return Task.CompletedTask;
                });

            await _gateway.AddEventCallbackAsync<NotificationSubscriptionDto[]>("ReceiveWebPushSubscriptions",
                subscriptions =>
                {
                    _callbackExecutor.ExecuteSubscriptionsByName(subscriptions, "ReceiveWebPushSubscriptions");
                    return Task.CompletedTask;
                });

            await _gateway.AddEventCallbackAsync<NotificationSubscriptionDto[]>("RemovedFromWebPushSubscriptions",
                removedSubscriptions =>
                {
                    _callbackExecutor.ExecuteSubscriptionsByName(removedSubscriptions,
                        "RemovedFromWebPushSubscriptions");
                    
                    return Task.CompletedTask;
                });

            await _gateway.AddEventCallbackAsync("WebPushSubscriptionSetChanged",
                () => 
                {
                    _callbackExecutor.ExecuteSubscriptionsByName("WebPushSubscriptionSetChanged");
                    return Task.CompletedTask;
                });

            await _gateway.AddEventCallbackAsync<IsUserExistDto>("UserExistanceResponse",
                async isUserExistDTO =>
                {
                    _callbackExecutor.ExecuteSubscriptionsByName(isUserExistDTO, "UserExistanceResponse");
                });
        }

        public async Task ActualizeConnectedUsersAsync()
        {
            var gateway = _gateway ?? await ConfigureGateway();
            await gateway.SendAsync("PushOnlineUsersToClients");
        }

        public async Task CheckIfUserOnline(string username)
        {
            var gateway = _gateway ?? await ConfigureGateway();
            await gateway.SendAsync("IsUserOnline", username);
        }

        public async Task AddUserWebPushSubscription(NotificationSubscriptionDto subscriptionDTO)
        {
            var gateway = _gateway ?? await ConfigureGateway();
            await gateway.SendAsync("AddUserWebPushSubscription", subscriptionDTO);
        }

        public async Task GetUserWebPushSubscriptions(CredentialsDTO credentialsDto)
        {
            var gateway = _gateway ?? await ConfigureGateway();
            await gateway.SendAsync("GetUserWebPushSubscriptions", credentialsDto);
        }

        public async Task RemoveUserWebPushSubscriptions(NotificationSubscriptionDto[] subscriptionsToRemove)
        {
            if (subscriptionsToRemove.All(x => x.JwtPair is null && x.WebAuthnPair is null))
                throw new ArgumentException
                ($"At least one of parameters array " +
                 $"should have it's {nameof(NotificationSubscriptionDto.WebAuthnPair)} or {nameof(NotificationSubscriptionDto.JwtPair)} not null");

            var gateway = _gateway ?? await ConfigureGateway();
            await gateway.SendAsync("RemoveUserWebPushSubscriptions", subscriptionsToRemove);
        }

        public async Task CheckIfUserExists(string username)
        {
            var gateway = _gateway ?? await ConfigureGateway();
            await gateway.SendAsync("CheckIfUserExist", username);
        }
    }
}