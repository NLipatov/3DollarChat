using Limp.Client.Cryptography.KeyStorage;
using Limp.Client.HubInteraction.Handlers.Helpers;
using Limp.Client.Services.HubServices.CommonServices;
using Limp.Client.Services.HubServices.CommonServices.CallbackExecutor;
using LimpShared.Encryption;
using LimpShared.Models.ConnectedUsersManaging;
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
        private HubConnection? hubConnection = null;
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
        public async Task<HubConnection> ConnectAsync()
        {
            HubConnection? existingHubConnection = await TryGetExistingHubConnection();
            if (existingHubConnection != null && existingHubConnection.State == HubConnectionState.Connected)
            {
                return existingHubConnection;
            }

            hubConnection = new HubConnectionBuilder()
            .WithUrl(_navigationManager.ToAbsoluteUri("/usersHub"))
            .Build();

            //Here we are registering a callbacks for specific server-triggered events.
            //Events are being triggered from SignalR hubs in server project.
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

            await hubConnection.StartAsync();

            await hubConnection.SendAsync("SetUsername", await JWTHelper.GetAccessTokenAsync(_jSRuntime));

            return hubConnection;
        }
        private async Task<HubConnection?> TryGetExistingHubConnection()
        {
            if (hubConnection != null)
            {
                if (hubConnection.State != HubConnectionState.Connected)
                {
                    await hubConnection.StopAsync();
                    await hubConnection.StartAsync();
                }
                return hubConnection;
            }
            return null;
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

        public async Task SubscribeToWebPushAsync(NotificationSubscriptionDTO subscriptionDTO)
        {
            if (hubConnection?.State is not HubConnectionState.Connected)
                throw new ApplicationException("Hub is not connected.");

            await hubConnection.SendAsync("SubscribeToWebPushNotifications", subscriptionDTO);
        }
    }
}
