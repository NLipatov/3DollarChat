using Limp.Client.Cryptography.KeyStorage;
using Limp.Client.HubInteraction.Handlers.Helpers;
using Limp.Client.Services.HubServices.CommonServices;
using Limp.Client.Services.HubServices.CommonServices.CallbackExecutor;
using Limp.Client.Services.HubServices.MessageService.Implementation;
using LimpShared.Encryption;
using LimpShared.Models.ConnectedUsersManaging;
using LimpShared.Models.Users;
using LimpShared.Models.WebPushNotification;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using System.Collections.Concurrent;

namespace Limp.Client.Services.HubService.UsersService.Implementation
{
    public class UsersService : IUsersService
    {
        private readonly IJSRuntime _jSRuntime;
        private readonly NavigationManager _navigationManager;
        private readonly ICallbackExecutor _callbackExecutor;
        private HubConnection? hubConnection { get; set; }

        private ConcurrentDictionary<Guid, Func<string, Task>> ConnectionIdReceivedCallbacks = new();
        private ConcurrentDictionary<Guid, Func<string, Task>> UsernameResolvedCallbacks = new();
        public UsersService
        (IJSRuntime jSRuntime,
        NavigationManager navigationManager,
        ICallbackExecutor callbackExecutor)
        {
            _jSRuntime = jSRuntime;
            _navigationManager = navigationManager;
            _callbackExecutor = callbackExecutor;
        }

        public async Task<HubConnection?> ConnectAsync()
        {
            string? accessToken = await JWTHelper.GetAccessTokenAsync(_jSRuntime);

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                _navigationManager.NavigateTo("signin");
                return null;
            }

            if (hubConnection?.State == HubConnectionState.Connected)
            {
                return hubConnection;
            }
            else
            {
                if (hubConnection is null)
                {
                    InitializeHubConnection();
                    RegisterHubEventHandlers();
                }
                else
                {
                    await hubConnection.DisposeAsync();
                    InitializeHubConnection();
                    RegisterHubEventHandlers();
                }
            }

            if (hubConnection == null)
                throw new ArgumentException($"{nameof(UsersService)} {nameof(hubConnection)} initialization failed. {nameof(hubConnection)} is null.");

            await hubConnection.StartAsync();

            await hubConnection.SendAsync("SetUsername", accessToken);

            hubConnection.Closed += OnConnectionLost;

            return hubConnection;
        }

        private async Task OnConnectionLost(Exception? exception)
        {
            await Console.Out.WriteLineAsync("UsersHub connection lost. Reconnecting.");
            await ConnectAsync();
        }

        private void RegisterHubEventHandlers()
        {
            if (hubConnection is null)
                throw new NullReferenceException($"Could not register event handlers - hub was null.");

            hubConnection.On<UserConnectionsReport>("ReceiveOnlineUsers", updatedTrackedUserConnections =>
            {
                _callbackExecutor.ExecuteSubscriptionsByName(updatedTrackedUserConnections, "ReceiveOnlineUsers");
            });

            hubConnection.On<string>("ReceiveConnectionId", connectionId =>
            {
                _callbackExecutor.ExecuteCallbackDictionary(connectionId, ConnectionIdReceivedCallbacks);
            });

            hubConnection.On<string>("OnNameResolve", async username =>
            {
                _callbackExecutor.ExecuteCallbackDictionary(username, UsernameResolvedCallbacks);

                await hubConnection.SendAsync("PostAnRSAPublic", username, InMemoryKeyStorage.MyRSAPublic.Value);
            });

            hubConnection.On<UserConnection>("IsUserOnlineResponse", (UserConnection) =>
            {
                _callbackExecutor.ExecuteSubscriptionsByName(UserConnection, "IsUserOnlineResponse");
            });

            hubConnection.On<NotificationSubscriptionDTO[]>("ReceiveWebPushSubscriptions", subscriptions =>
            {
                _callbackExecutor.ExecuteSubscriptionsByName(subscriptions, "ReceiveWebPushSubscriptions");
            });

            hubConnection.On<NotificationSubscriptionDTO[]>("RemovedFromWebPushSubscriptions", removedSubscriptions =>
            {
                _callbackExecutor.ExecuteSubscriptionsByName(removedSubscriptions, "RemovedFromWebPushSubscriptions");
            });

            hubConnection.On("WebPushSubscriptionSetChanged", () =>
            {
                _callbackExecutor.ExecuteSubscriptionsByName("WebPushSubscriptionSetChanged");
            });

            hubConnection.On<IsUserExistDTO>("UserExistanceResponse", async isUserExistDTO =>
            {
                _callbackExecutor.ExecuteSubscriptionsByName(isUserExistDTO, "UserExistanceResponse");
            });
        }

        private void InitializeHubConnection()
        {
            hubConnection = new HubConnectionBuilder()
            .WithUrl(_navigationManager.ToAbsoluteUri("/usersHub"))
            .AddMessagePackProtocol()
            .Build();
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

        public async Task DisconnectAsync()
        {
            await HubDisconnecter.DisconnectAsync(hubConnection);
            hubConnection = null;
        }

        public async Task SetRSAPublicKey(string accessToken, Key RSAPublicKey)
        {
            if (hubConnection != null)
            {
                await hubConnection.SendAsync("SetRSAPublicKey", accessToken, RSAPublicKey);
                InMemoryKeyStorage.isPublicKeySet = true;
            }
            else
            {
                await ReconnectAsync();
            }
        }

        public async Task ActualizeConnectedUsersAsync()
        {
            if (hubConnection != null)
            {
                await hubConnection.SendAsync("PushOnlineUsersToClients");
            }
            else
            {
                await ReconnectAsync();
            }
        }

        public async Task ReconnectAsync()
        {
            await DisconnectAsync();
            await ConnectAsync();
        }

        public async Task CheckIfUserOnline(string username)
        {
            if (hubConnection != null)
            {
                await hubConnection.SendAsync("IsUserOnline", username);
            }
            else
            {
                await ReconnectAsync();
            }
        }

        public async Task AddUserWebPushSubscription(NotificationSubscriptionDTO subscriptionDTO)
        {
            if (hubConnection?.State is not HubConnectionState.Connected)
                throw new ApplicationException("Hub is not connected.");

            await hubConnection.SendAsync("AddUserWebPushSubscription", subscriptionDTO);
        }

        public async Task GetUserWebPushSubscriptions(string accessToken)
        {
            if (hubConnection?.State is not HubConnectionState.Connected)
                throw new ApplicationException("Hub is not connected.");

            await hubConnection.SendAsync("GetUserWebPushSubscriptions", accessToken);
        }

        public async Task RemoveUserWebPushSubscriptions(NotificationSubscriptionDTO[] subscriptionsToRemove)
        {
            //If there are no subscription with access token - throw an exception
            if (!subscriptionsToRemove.Any(x => !string.IsNullOrWhiteSpace(x.AccessToken)))
                throw new ArgumentException
                    ($"Atleast one of parameters array should have it's {nameof(NotificationSubscriptionDTO.AccessToken)} not null");

            if (hubConnection?.State is not HubConnectionState.Connected)
                throw new ApplicationException("Hub is not connected.");

            await hubConnection.SendAsync("RemoveUserWebPushSubscriptions", subscriptionsToRemove);
        }

        public async Task CheckIfUserExists(string username)
        {
            if (hubConnection?.State is not HubConnectionState.Connected)
                throw new ApplicationException("Hub is not connected.");

            await hubConnection.SendAsync("CheckIfUserExist", username);
        }
    }
}
