using System.Collections.Concurrent;
using Client.Application.Gateway;
using Client.Infrastructure.Gateway;
using Ethachat.Client.Services.Authentication.Handlers;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;
using EthachatShared.Constants;
using EthachatShared.Models.Authentication.Models.Credentials.CredentialsDTO;
using EthachatShared.Models.ConnectedUsersManaging;
using EthachatShared.Models.Message;
using EthachatShared.Models.Users;
using EthachatShared.Models.WebPushNotification;
using MessagePack;
using Microsoft.AspNetCore.Components;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.UsersService.Implementation
{
    public class UsersService(
        NavigationManager navigationManager,
        ICallbackExecutor callbackExecutor,
        IAuthenticationHandler authenticationHandler)
        : IUsersService
    {
        private readonly ConcurrentDictionary<Guid, Func<string, Task>> _connectionIdReceivedCallbacks = new();
        private readonly ConcurrentDictionary<Guid, Func<string, Task>> _usernameResolvedCallbacks = new();
        private IGateway? _gateway;

        private async Task<IGateway> ConfigureGateway()
        {
            var gateway = new SignalRGateway();
            await gateway.ConfigureAsync(navigationManager.ToAbsoluteUri(HubAddress.Users),
                async () => await authenticationHandler.GetCredentialsDto());
            return gateway;
        }


        public async Task<IGateway> GetHubConnectionAsync()
        {
            if (!await authenticationHandler.IsSetToUseAsync())
            {
                navigationManager.NavigateTo("signin");
            }

            return await InitializeGatewayAsync();
        }

        private async Task<IGateway> InitializeGatewayAsync()
        {
            _gateway ??= await ConfigureGateway();

            await _gateway.AddEventCallbackAsync<UserConnectionsReport>("ReceiveOnlineUsers",
                updatedTrackedUserConnections =>
                {
                    callbackExecutor.ExecuteSubscriptionsByName(updatedTrackedUserConnections, "ReceiveOnlineUsers");
                    return Task.CompletedTask;
                });

            await _gateway.AddEventCallbackAsync<string>("ReceiveConnectionId",
                connectionId =>
                {
                    callbackExecutor.ExecuteCallbackDictionary(connectionId, _connectionIdReceivedCallbacks);
                    return Task.CompletedTask;
                });

            await _gateway.AddEventCallbackAsync<string>("OnNameResolve", username =>
            {
                callbackExecutor.ExecuteSubscriptionsByName(username, "OnNameResolve");

                callbackExecutor.ExecuteCallbackDictionary(username, _usernameResolvedCallbacks);
                return Task.CompletedTask;
            });

            await _gateway.AddEventCallbackAsync<UserConnection>("IsUserOnlineResponse",
                (userConnection) =>
                {
                    callbackExecutor.ExecuteSubscriptionsByName(userConnection, "IsUserOnlineResponse");
                    return Task.CompletedTask;
                });

            await _gateway.AddEventCallbackAsync<NotificationSubscriptionDto[]>("ReceiveWebPushSubscriptions",
                subscriptions =>
                {
                    callbackExecutor.ExecuteSubscriptionsByName(subscriptions, "ReceiveWebPushSubscriptions");
                    return Task.CompletedTask;
                });

            await _gateway.AddEventCallbackAsync<NotificationSubscriptionDto[]>("RemovedFromWebPushSubscriptions",
                removedSubscriptions =>
                {
                    callbackExecutor.ExecuteSubscriptionsByName(removedSubscriptions,
                        "RemovedFromWebPushSubscriptions");

                    return Task.CompletedTask;
                });

            await _gateway.AddEventCallbackAsync("WebPushSubscriptionSetChanged",
                () =>
                {
                    callbackExecutor.ExecuteSubscriptionsByName("WebPushSubscriptionSetChanged");
                    return Task.CompletedTask;
                });

            await _gateway.AddEventCallbackAsync<IsUserExistDto>("UserExistanceResponse", isUserExistDto =>
            {
                callbackExecutor.ExecuteSubscriptionsByName(isUserExistDto, "UserExistanceResponse");
                return Task.CompletedTask;
            });

            return _gateway;
        }

        public async Task ActualizeConnectedUsersAsync()
        {
            var gateway = _gateway ?? await ConfigureGateway();
            await gateway.TransferAsync(new ClientToServerData
            {
                Id = Guid.NewGuid(),
                EventName = "PushOnlineUsersToClients"
            });
        }

        public async Task CheckIfUserOnline(string username)
        {
            var gateway = _gateway ?? await ConfigureGateway();
            await gateway.TransferAsync(new ClientToServerData
            {
                Id = Guid.NewGuid(),
                EventName = "IsUserOnline",
                Type = typeof(string),
                Data = MessagePackSerializer.Serialize(username)
            });
        }

        public async Task AddUserWebPushSubscription(NotificationSubscriptionDto subscriptionDto)
        {
            var gateway = _gateway ?? await ConfigureGateway();
            await gateway.TransferAsync(new ClientToServerData
            {
                EventName = "AddUserWebPushSubscription",
                Data = MessagePackSerializer.Serialize(subscriptionDto),
                Type = typeof(string),
            });
        }

        public async Task GetUserWebPushSubscriptions(CredentialsDTO credentialsDto)
        {
            var gateway = _gateway ?? await ConfigureGateway();
            await gateway.TransferAsync(new ClientToServerData
            {
                EventName = "GetUserWebPushSubscriptions",
                Data = MessagePackSerializer.Serialize(credentialsDto),
                Type = typeof(CredentialsDTO)
            });
        }

        public async Task RemoveUserWebPushSubscriptions(NotificationSubscriptionDto[] subscriptionsToRemove)
        {
            if (subscriptionsToRemove.All(x => x.JwtPair is null && x.WebAuthnPair is null))
                throw new ArgumentException
                ($"At least one of parameters array " +
                 $"should have it's {nameof(NotificationSubscriptionDto.WebAuthnPair)} or {nameof(NotificationSubscriptionDto.JwtPair)} not null");

            var gateway = _gateway ?? await ConfigureGateway();
            await gateway.TransferAsync(new ClientToServerData
            {
                EventName = "RemoveUserWebPushSubscriptions",
                Data = MessagePackSerializer.Serialize(subscriptionsToRemove),
                Type = typeof(NotificationSubscriptionDto[]),
            });
        }

        public async Task CheckIfUserExists(string username)
        {
            var gateway = _gateway ?? await ConfigureGateway();
            await gateway.TransferAsync(new ClientToServerData
            {
                EventName = "CheckIfUserExist",
                Data = MessagePackSerializer.Serialize(username),
                Type = typeof(string),
            });
        }
    }
}