using ClientServerCommon.Models;
using Limp.Client.Cryptography.KeyStorage;
using Limp.Client.HubInteraction.Handlers.Helpers;
using Limp.Client.Services.HubService.CommonServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Limp.Client.Services.HubService.UsersService.Implementation
{
    public class UsersService : IUsersService
    {
        private readonly IJSRuntime _jSRuntime;
        private readonly NavigationManager _navigationManager;
        private HubConnection? usersHubConnection = null;
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
            if (usersHubConnection != null)
            {
                if (usersHubConnection.State != HubConnectionState.Connected)
                {
                    await usersHubConnection.StopAsync();
                    await usersHubConnection.StartAsync();
                }
                return usersHubConnection;
            }

            usersHubConnection = new HubConnectionBuilder()
            .WithUrl(_navigationManager.ToAbsoluteUri("/usersHub"))
            .Build();

            //Here we are registering a callbacks for specific server-triggered events.
            //Events are being triggered from SignalR hubs in server project.
            usersHubConnection.On<List<UserConnection>>("ReceiveOnlineUsers", updatedTrackedUserConnections =>
            {
                CallbackExecutor.ExecuteCallbackDictionary(updatedTrackedUserConnections, UsersOnlineUpdateCallbacks);
                //await _usersHubObserver.CallHandler(UsersHubEvent.ConnectedUsersListReceived, updatedTrackedUserConnections);
            });

            usersHubConnection.On<string>("ReceiveConnectionId", connectionId =>
            {
                CallbackExecutor.ExecuteCallbackDictionary(connectionId, ConnectionIdReceivedCallbacks);
                //await _usersHubObserver.CallHandler(UsersHubEvent.ConnectionIdReceived, connectionId);
            });

            usersHubConnection.On<string>("onNameResolve", async username =>
            {
                CallbackExecutor.ExecuteCallbackDictionary(username, UsernameResolvedCallbacks);
                //await _usersHubObserver.CallHandler(UsersHubEvent.MyUsernameResolved, username);

                await usersHubConnection.SendAsync("PostAnRSAPublic", username, InMemoryKeyStorage.MyRSAPublic.Value);
            });

            await usersHubConnection.StartAsync();

            await usersHubConnection.SendAsync("SetUsername", await JWTHelper.GetAccessToken(_jSRuntime));

            return usersHubConnection;
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
    }
}
