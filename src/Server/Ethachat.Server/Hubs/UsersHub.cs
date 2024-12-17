using Ethachat.Server.Hubs.UsersConnectedManaging.ConnectedUserStorage;
using Ethachat.Server.Hubs.UsersConnectedManaging.EventHandling.OnlineUsersRequestEvent;
using Ethachat.Server.Utilities.HttpMessaging;
using Ethachat.Server.Utilities.UsernameResolver;
using EthachatShared.Models.Authentication.Models;
using EthachatShared.Models.Authentication.Models.Credentials.CredentialsDTO;
using EthachatShared.Models.ConnectedUsersManaging;
using EthachatShared.Models.Message;
using EthachatShared.Models.Users;
using EthachatShared.Models.WebPushNotification;
using Microsoft.AspNetCore.SignalR;
using SharedServices;

namespace Ethachat.Server.Hubs
{
    public class UsersHub(
        IServerHttpClient serverHttpClient,
        IOnlineUsersManager onlineUsersManager,
        IUsernameResolverService usernameResolverService,
        ISerializerService serializerService)
        : Hub
    {
        public override async Task OnConnectedAsync()
        {
            InMemoryHubConnectionStorage
                .UsersHubConnections
                .TryAdd(Context.ConnectionId, new List<string> { Context.ConnectionId });

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var keys = InMemoryHubConnectionStorage
                .UsersHubConnections
                .Where(x => x.Value.Contains(Context.ConnectionId))
                .Select(x => x.Key);

            foreach (var key in keys)
            {
                var oldConnections = InMemoryHubConnectionStorage.UsersHubConnections[key];
                var newConnections = oldConnections
                    .Where(x => x != Context.ConnectionId)
                    .ToList();
                
                if (newConnections.Any())
                    InMemoryHubConnectionStorage.UsersHubConnections.TryUpdate(key, newConnections, oldConnections);
                else
                    InMemoryHubConnectionStorage.UsersHubConnections.TryRemove(key, out _);

                await SendOnlineUsersToClients();
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SetUsername(CredentialsDTO credentialsDto)
        {
            AuthResult usernameRequestResult = await usernameResolverService.GetUsernameAsync(credentialsDto);
            if (usernameRequestResult.Result is not AuthResultType.Success)
            {
                await Clients.Caller.SendAsync("OnAccessTokenInvalid", usernameRequestResult);
            }

            var keys = InMemoryHubConnectionStorage
                .UsersHubConnections
                .Where(x => x.Value
                    .Contains(Context.ConnectionId))
                .Select(x => x.Key);

            foreach (var key in keys)
            {
                var connections = InMemoryHubConnectionStorage.UsersHubConnections[key];
                InMemoryHubConnectionStorage.UsersHubConnections.TryRemove(key, out _);
                InMemoryHubConnectionStorage.UsersHubConnections.TryAdd(
                    usernameRequestResult.Message ?? throw new NullReferenceException(usernameRequestResult.Message), 
                    connections);

                await SendOnlineUsersToClients();
            }
        }

        private async Task SendOnlineUsersToClients()
        {
            //Defines a set of clients that are connected to both UsersHub and MessageDispatcherHub at the same time
            UserConnectionsReport report = onlineUsersManager.FormUsersOnlineMessage();
            //Pushes set of clients to all the clients
            await Clients.All.SendAsync("ReceiveOnlineUsers", report);
        }

        public async Task PushOnlineUsersToClients(ClientToServerData data)
        {
            await Clients.All.SendAsync("OnClientToServerDataAck", data.Id);
            //Defines a set of clients that are connected to both UsersHub and MessageDispatcherHub at the same time
            UserConnectionsReport report = onlineUsersManager.FormUsersOnlineMessage();
            //Pushes set of clients to all the clients
            await Clients.All.SendAsync("ReceiveOnlineUsers", report);
        }

        public async Task PushConId()
        {
            await Clients.Caller.SendAsync("ReceiveConnectionId", Context.ConnectionId);
        }

        public async Task IsUserOnline(ClientToServerData data)
        {
            await Clients.All.SendAsync("OnClientToServerDataAck", data.Id);
            var username = await serializerService.DeserializeAsync<string>(data.Data);
            string[] userHubConnections =
                InMemoryHubConnectionStorage.UsersHubConnections
                .Where(x => x.Key == username)
                .SelectMany(x => x.Value)
                .ToArray();

            string[] messageHubConnections =
                InMemoryHubConnectionStorage.MessageDispatcherHubConnections
                .Where(x => x.Key == username)
                .SelectMany(x => x.Value)
                .ToArray();

            bool isOnline = userHubConnections.Length > 0 && messageHubConnections.Length > 0;

            await Clients.Caller.SendAsync("IsUserOnlineResponse", new UserConnection
            {
                Username = username,
                ConnectionIds = isOnline ? messageHubConnections : new string[0],
            });
        }

        public async Task AddUserWebPushSubscription(ClientToServerData data)
        {
            await Clients.All.SendAsync("OnClientToServerDataAck", data.Id);
            var notificationSubscriptionDto = await serializerService
                .DeserializeAsync<NotificationSubscriptionDto>(data.Data);
            await serverHttpClient.AddUserWebPushSubscribtion(notificationSubscriptionDto);
            await Clients.Caller.SendAsync("WebPushSubscriptionSetChanged");
        }

        public async Task GetUserWebPushSubscriptions(ClientToServerData data)
        {
            await Clients.All.SendAsync("OnClientToServerDataAck", data.Id);
            var credentialsDto = await serializerService.DeserializeAsync<CredentialsDTO>(data.Data);
            var userRequestResult = await usernameResolverService.GetUsernameAsync(credentialsDto);

            if (userRequestResult.Result is not AuthResultType.Success)
                throw new ArgumentException("Access token was not valid");

            var username = userRequestResult.Message ?? string.Empty;

            var userSubscriptions = await serverHttpClient
                .GetUserWebPushSubscriptionsByAccessToken(username);
            await Clients.Caller.SendAsync("ReceiveWebPushSubscriptions", userSubscriptions);
        }

        public async Task RemoveUserWebPushSubscriptions(ClientToServerData data)
        {
            await Clients.All.SendAsync("OnClientToServerDataAck", data.Id);
            var notificationSubscriptionDtOs = await serializerService
                .DeserializeAsync<NotificationSubscriptionDto[]>(data.Data);
            await serverHttpClient.RemoveUserWebPushSubscriptions(notificationSubscriptionDtOs);
            await Clients.Caller.SendAsync("RemovedFromWebPushSubscriptions", notificationSubscriptionDtOs);
            await Clients.Caller.SendAsync("WebPushSubscriptionSetChanged");
        }

        public async Task CheckIfUserExist(ClientToServerData data)
        {
            await Clients.All.SendAsync("OnClientToServerDataAck", data.Id);
            var username = await serializerService.DeserializeAsync<string>(data.Data);
            IsUserExistDto response = await serverHttpClient.CheckIfUserExists(username);
            await Clients.Caller.SendAsync("UserExistanceResponse", response);
        }
    }
}