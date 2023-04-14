using ClientServerCommon.Models;
using Limp.Client.Cryptography.KeyStorage;
using Limp.Client.HubInteraction.Handlers.Helpers;
using Limp.Client.Services.HubService.CommonServices;
using Limp.Client.Services.HubServices.CommonServices;
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
        private HubConnection? hubConnection = null;
        private ConcurrentDictionary<Guid, Func<List<UserConnection>, Task>> UsersOnlineUpdateCallbacks = new();
        private ConcurrentDictionary<Guid, Func<string, Task>> ConnectionIdReceivedCallbacks = new();
        private ConcurrentDictionary<Guid, Func<string, Task>> UsernameResolvedCallbacks = new();
        public UsersService
        (IJSRuntime jSRuntime,
        NavigationManager navigationManager)
        {
            _jSRuntime = jSRuntime;
            _navigationManager = navigationManager;
        }
        public async Task<HubConnection> ConnectAsync()
        {
            HubConnection? existingHubConnection = await TryGetExistingHubConnection();
            if (existingHubConnection != null)
            {
                return existingHubConnection;
            }

            hubConnection = new HubConnectionBuilder()
            .WithUrl(_navigationManager.ToAbsoluteUri("/usersHub"))
            .Build();

            //Here we are registering a callbacks for specific server-triggered events.
            //Events are being triggered from SignalR hubs in server project.
            hubConnection.On<List<UserConnection>>("ReceiveOnlineUsers", updatedTrackedUserConnections =>
            {
                CallbackExecutor.ExecuteCallbackDictionary(updatedTrackedUserConnections, UsersOnlineUpdateCallbacks);
            });

            hubConnection.On<string>("ReceiveConnectionId", connectionId =>
            {
                CallbackExecutor.ExecuteCallbackDictionary(connectionId, ConnectionIdReceivedCallbacks);
            });

            hubConnection.On<string>("onNameResolve", async username =>
            {
                CallbackExecutor.ExecuteCallbackDictionary(username, UsernameResolvedCallbacks);

                await hubConnection.SendAsync("PostAnRSAPublic", username, InMemoryKeyStorage.MyRSAPublic.Value);
            });

            await hubConnection.StartAsync();

            await hubConnection.SendAsync("SetUsername", await JWTHelper.GetAccessToken(_jSRuntime));

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
                RemoveSubsctiptionToUsersOnlineUpdate(subscriptionId);
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

        public void RemoveSubsctiptionToUsersOnlineUpdate(Guid subscriptionId)
        {
            bool isRemoved = UsersOnlineUpdateCallbacks.Remove(subscriptionId, out _);
            if (!isRemoved)
            {
                RemoveSubsctiptionToUsersOnlineUpdate(subscriptionId);
            }
        }

        public Guid SubscribeToUsersOnlineUpdate(Func<List<UserConnection>, Task> callback)
        {
            Guid subscriptionId = Guid.NewGuid();
            bool isAdded = UsersOnlineUpdateCallbacks.TryAdd(subscriptionId, callback);
            if (!isAdded)
            {
                SubscribeToUsersOnlineUpdate(callback);
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
            bool isRemoved = UsersOnlineUpdateCallbacks.Remove(subscriptionId, out _);
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
    }
}
