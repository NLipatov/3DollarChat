using System.Collections.Concurrent;
using Limp.Client.Cryptography.KeyStorage;
using Limp.Client.Services.AuthenticationService.Handlers;
using Limp.Client.Services.HubServices.CommonServices.CallbackExecutor;
using Limp.Client.Services.HubServices.CommonServices.HubServiceConnectionBuilder;
using LimpShared.Encryption;
using LimpShared.Models.Authentication.Models;
using LimpShared.Models.Authentication.Models.Credentials;
using LimpShared.Models.Authentication.Models.Credentials.Implementation;
using LimpShared.Models.ConnectedUsersManaging;
using LimpShared.Models.Users;
using LimpShared.Models.WebPushNotification;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace Limp.Client.Services.HubServices.HubServices.Implementations.UsersService.Implementation
{
    public class UsersService : IUsersService
    {
        private readonly IJSRuntime _jSRuntime;
        public NavigationManager NavigationManager { get; set; }
        private readonly ICallbackExecutor _callbackExecutor;
        private readonly IAuthenticationHandler _authenticationHandler;
        private HubConnection? HubConnectionInstance { get; set; }

        private ConcurrentDictionary<Guid, Func<string, Task>> ConnectionIdReceivedCallbacks = new();
        private ConcurrentDictionary<Guid, Func<string, Task>> UsernameResolvedCallbacks = new();

        public UsersService
        (IJSRuntime jSRuntime,
            NavigationManager navigationManager,
            ICallbackExecutor callbackExecutor,
            IAuthenticationHandler authenticationHandler)
        {
            _jSRuntime = jSRuntime;
            NavigationManager = navigationManager;
            _callbackExecutor = callbackExecutor;
            _authenticationHandler = authenticationHandler;
            InitializeHubConnection();
            RegisterHubEventHandlers();
        }

        private void InitializeHubConnection()
        {
            if (HubConnectionInstance is not null)
                return;

            HubConnectionInstance = HubServiceConnectionBuilder
                .Build(NavigationManager.ToAbsoluteUri("/usersHub"));
        }

        private void RegisterHubEventHandlers()
        {
            if (HubConnectionInstance is null)
                throw new NullReferenceException($"Could not register event handlers - hub was null.");

            HubConnectionInstance.On<UserConnectionsReport>("ReceiveOnlineUsers",
                updatedTrackedUserConnections =>
                {
                    _callbackExecutor.ExecuteSubscriptionsByName(updatedTrackedUserConnections, "ReceiveOnlineUsers");
                });

            HubConnectionInstance.On<string>("ReceiveConnectionId",
                connectionId =>
                {
                    _callbackExecutor.ExecuteCallbackDictionary(connectionId, ConnectionIdReceivedCallbacks);
                });

            HubConnectionInstance.On<string>("OnNameResolve", async username =>
            {
                _callbackExecutor.ExecuteCallbackDictionary(username, UsernameResolvedCallbacks);

                await GetHubConnectionAsync();

                await HubConnectionInstance.SendAsync("PostAnRSAPublic", username,
                    InMemoryKeyStorage.MyRSAPublic.Value);
            });

            HubConnectionInstance.On<UserConnection>("IsUserOnlineResponse",
                (UserConnection) =>
                {
                    _callbackExecutor.ExecuteSubscriptionsByName(UserConnection, "IsUserOnlineResponse");
                });

            HubConnectionInstance.On<NotificationSubscriptionDto[]>("ReceiveWebPushSubscriptions",
                subscriptions =>
                {
                    _callbackExecutor.ExecuteSubscriptionsByName(subscriptions, "ReceiveWebPushSubscriptions");
                });

            HubConnectionInstance.On<NotificationSubscriptionDto[]>("RemovedFromWebPushSubscriptions",
                removedSubscriptions =>
                {
                    _callbackExecutor.ExecuteSubscriptionsByName(removedSubscriptions,
                        "RemovedFromWebPushSubscriptions");
                });

            HubConnectionInstance.On("WebPushSubscriptionSetChanged",
                () => { _callbackExecutor.ExecuteSubscriptionsByName("WebPushSubscriptionSetChanged"); });

            HubConnectionInstance.On<IsUserExistDto>("UserExistanceResponse",
                async isUserExistDTO =>
                {
                    _callbackExecutor.ExecuteSubscriptionsByName(isUserExistDTO, "UserExistanceResponse");
                });
        }

        public async Task<HubConnection> GetHubConnectionAsync()
        {
            var isAuthenticationIsReadyToUse = await _authenticationHandler.IsSetToUseAsync();
            if (!isAuthenticationIsReadyToUse)
            {
                NavigationManager.NavigateTo("signin");
                return null;
            }

            if (HubConnectionInstance == null)
                throw new ArgumentException($"{nameof(HubConnectionInstance)} was not properly instantiated.");

            while (HubConnectionInstance.State is not HubConnectionState.Connected)
            {
                try
                {
                    if (HubConnectionInstance.State is not HubConnectionState.Disconnected)
                        await HubConnectionInstance.StopAsync();

                    await HubConnectionInstance.StartAsync();
                }
                catch
                {
                    return await GetHubConnectionAsync();
                }
            }

            await HubConnectionInstance.SendAsync("SetUsername", await _authenticationHandler.GetAccessCredential());

            HubConnectionInstance.Closed += OnConnectionLost;

            return HubConnectionInstance;
        }

        private async Task OnConnectionLost(Exception? exception)
        {
            await GetHubConnectionAsync();
        }

        public void RemoveConnectionIdReceived(Guid subscriptionId)
        {
            bool isRemoved = ConnectionIdReceivedCallbacks.Remove(subscriptionId, out _);
            if (!isRemoved)
            {
                RemoveConnectionIdReceived(subscriptionId);
            }
        }

        public Guid SubscribeToConnectionIdReceived(Func<string, Task> callback)
        {
            Guid subscriptionId = Guid.NewGuid();
            bool isAdded = ConnectionIdReceivedCallbacks.TryAdd(subscriptionId, callback);
            if (!isAdded)
            {
                SubscribeToConnectionIdReceived(callback);
            }

            return subscriptionId;
        }

        public Guid SubscribeToUsernameResolved(Func<string, Task> callback)
        {
            Guid subscriptionId = Guid.NewGuid();
            bool isAdded = UsernameResolvedCallbacks.TryAdd(subscriptionId, callback);
            if (!isAdded)
            {
                SubscribeToUsernameResolved(callback);
            }

            return subscriptionId;
        }

        public void RemoveUsernameResolved(Guid subscriptionId)
        {
            bool isRemoved = UsernameResolvedCallbacks.Remove(subscriptionId, out _);
            if (!isRemoved)
            {
                RemoveUsernameResolved(subscriptionId);
            }
        }

        public async Task SetRSAPublicKey(Key RSAPublicKey)
        {
            var credentials = await _authenticationHandler.GetCredentials();
            if (credentials is WebAuthnPair)
            {
                var hubConnection = await GetHubConnectionAsync();
                await hubConnection.SendAsync("SetRSAPublicKey", RSAPublicKey, credentials, null);
            }
            else if (credentials is JwtPair)
            {
                var hubConnection = await GetHubConnectionAsync();
                await hubConnection.SendAsync("SetRSAPublicKey", RSAPublicKey, null, credentials);
            }
            InMemoryKeyStorage.isPublicKeySet = true;
        }

        public async Task ActualizeConnectedUsersAsync()
        {
            var hubConnection = await GetHubConnectionAsync();
            await hubConnection.SendAsync("PushOnlineUsersToClients");
        }

        public async Task CheckIfUserOnline(string username)
        {
            var hubConnection = await GetHubConnectionAsync();
            await hubConnection.SendAsync("IsUserOnline", username);
        }

        public async Task AddUserWebPushSubscription(NotificationSubscriptionDto subscriptionDTO)
        {
            var hubConnection = await GetHubConnectionAsync();
            await hubConnection.SendAsync("AddUserWebPushSubscription", subscriptionDTO);
        }

        public async Task GetUserWebPushSubscriptions(string accessToken)
        {
            var hubConnection = await GetHubConnectionAsync();
            await hubConnection.SendAsync("GetUserWebPushSubscriptions", accessToken);
        }

        public async Task RemoveUserWebPushSubscriptions(NotificationSubscriptionDto[] subscriptionsToRemove)
        {
            if (subscriptionsToRemove.All(x => string.IsNullOrWhiteSpace(x.AccessToken)))
                throw new ArgumentException
                ($"At least one of parameters array " +
                 $"should have it's {nameof(NotificationSubscriptionDto.AccessToken)} not null");

            var hubConnection = await GetHubConnectionAsync();
            await hubConnection.SendAsync("RemoveUserWebPushSubscriptions", subscriptionsToRemove);
        }

        public async Task CheckIfUserExists(string username)
        {
            var hubConnection = await GetHubConnectionAsync();
            await hubConnection.SendAsync("CheckIfUserExist", username);
        }
    }
}