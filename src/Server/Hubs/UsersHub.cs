using Ethachat.Client.Extensions;
using Ethachat.Server.Hubs.UsersConnectedManaging.ConnectedUserStorage;
using Ethachat.Server.Hubs.UsersConnectedManaging.EventHandling;
using Ethachat.Server.Hubs.UsersConnectedManaging.EventHandling.OnlineUsersRequestEvent;
using Ethachat.Server.Utilities.HttpMessaging;
using Ethachat.Server.Utilities.UsernameResolver;
using EthachatShared.Encryption;
using EthachatShared.Models.Authentication.Models;
using EthachatShared.Models.Authentication.Models.Credentials.CredentialsDTO;
using EthachatShared.Models.Authentication.Models.Credentials.Implementation;
using EthachatShared.Models.Authentication.Types;
using EthachatShared.Models.ConnectedUsersManaging;
using EthachatShared.Models.Message;
using EthachatShared.Models.Users;
using EthachatShared.Models.WebPushNotification;
using Microsoft.AspNetCore.SignalR;

namespace Ethachat.Server.Hubs
{
    public class UsersHub : Hub
    {
        private readonly IServerHttpClient _serverHttpClient;
        private readonly IUserConnectedHandler<UsersHub> _userConnectedHandler;
        private readonly IOnlineUsersManager _onlineUsersManager;
        private readonly IUsernameResolverService _usernameResolverService;

        public UsersHub
        (IServerHttpClient serverHttpClient,
            IUserConnectedHandler<UsersHub> userConnectedHandler,
            IOnlineUsersManager onlineUsersManager,
            IUsernameResolverService usernameResolverService)
        {
            _serverHttpClient = serverHttpClient;
            _userConnectedHandler = userConnectedHandler;
            _onlineUsersManager = onlineUsersManager;
            _usernameResolverService = usernameResolverService;
        }

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
                var newConnections = oldConnections.Where(x => x != Context.ConnectionId).ToList();
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
            AuthResult usernameRequestResult = await _usernameResolverService.GetUsernameAsync(credentialsDto);
            if (usernameRequestResult.Result is not AuthResultType.Success)
            {
                await Clients.Caller.SendAsync("OnAccessTokenInvalid", usernameRequestResult);
            }

            var usernameFromToken = usernameRequestResult.Message;

            var keys = InMemoryHubConnectionStorage
                .UsersHubConnections
                .Where(x => x.Value.Contains(Context.ConnectionId)).Select(x => x.Key);

            foreach (var key in keys)
            {
                var connections = InMemoryHubConnectionStorage.UsersHubConnections[key];
                InMemoryHubConnectionStorage.UsersHubConnections.TryRemove(key, out _);
                InMemoryHubConnectionStorage.UsersHubConnections.TryAdd(usernameFromToken, connections);

                await SendOnlineUsersToClients();
            }
        }

        public async Task SetRSAPublicKey(Key RSAPublicKey, WebAuthnPair? webAuthnPair = null, JwtPair? jwtPair = null)
        {
            try
            {
                bool isTokenValid = false;
                string username = string.Empty;
                AuthenticationType authenticationType = AuthenticationType.JwtToken;

                if (jwtPair is not null)
                {
                    authenticationType = AuthenticationType.JwtToken;
                    var validationResult =
                        await _serverHttpClient.ValidateCredentials(new CredentialsDTO() { JwtPair = jwtPair });
                    isTokenValid = validationResult.Result is AuthResultType.Success;
                    var userRequestResult = await _usernameResolverService.GetUsernameAsync(new CredentialsDTO
                        { JwtPair = jwtPair, WebAuthnPair = webAuthnPair });

                    if (userRequestResult.Result is not AuthResultType.Success)
                        throw new ArgumentException("Invalid JWT credentials");

                    username = userRequestResult.Message ?? string.Empty;
                }
                else if (webAuthnPair is not null)
                {
                    authenticationType = AuthenticationType.WebAuthn;
                    var validationResult =
                        await _serverHttpClient.ValidateCredentials(new CredentialsDTO { WebAuthnPair = webAuthnPair });
                    isTokenValid = validationResult.Result is AuthResultType.Success;
                    var userRequestResult = await _usernameResolverService.GetUsernameAsync(new CredentialsDTO
                        { JwtPair = jwtPair, WebAuthnPair = webAuthnPair });

                    if (userRequestResult.Result is not AuthResultType.Success)
                        throw new ArgumentException("Invalid WebAuthn credentials");

                    username = userRequestResult.Message ?? string.Empty;
                }

                await Clients.Caller.SendAsync("OnNameResolve", username);
            }
            catch (Exception e)
            {
                throw;
            }

            await SendOnlineUsersToClients();
        }

        private async Task OnUsernameResolvedHandlers(string username)
        {
            await SendOnlineUsersToClients();
            await PushConId();
            await PushResolvedName(username);
        }

        private async Task PushResolvedName(string username)
        {
            await Clients.Caller.SendAsync("OnNameResolve", username);
        }

        private async Task SendOnlineUsersToClients()
        {
            //Defines a set of clients that are connected to both UsersHub and MessageDispatcherHub at the same time
            UserConnectionsReport report = _onlineUsersManager.FormUsersOnlineMessage();
            //Pushes set of clients to all the clients
            await Clients.All.SendAsync("ReceiveOnlineUsers", report);
        }

        public async Task PushOnlineUsersToClients(ClientToServerData data)
        {
            await Clients.All.SendAsync("OnClientToServerDataAck", data.Id);
            //Defines a set of clients that are connected to both UsersHub and MessageDispatcherHub at the same time
            UserConnectionsReport report = _onlineUsersManager.FormUsersOnlineMessage();
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
            var username = await data.Data.DeserializeAsync<string>();
            string[] userHubConnections =
                InMemoryHubConnectionStorage.UsersHubConnections.Where(x => x.Key == username).SelectMany(x => x.Value)
                    .ToArray();

            string[] messageHubConnections =
                InMemoryHubConnectionStorage.MessageDispatcherHubConnections.Where(x => x.Key == username)
                    .SelectMany(x => x.Value).ToArray();

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
            var notificationSubscriptionDto = await data.Data.DeserializeAsync<NotificationSubscriptionDto>();
            await _serverHttpClient.AddUserWebPushSubscribtion(notificationSubscriptionDto);
            await Clients.Caller.SendAsync("WebPushSubscriptionSetChanged");
        }

        public async Task GetUserWebPushSubscriptions(ClientToServerData data)
        {
            await Clients.All.SendAsync("OnClientToServerDataAck", data.Id);
            var credentialsDto = await data.Data.DeserializeAsync<CredentialsDTO>();
            var userRequestResult = await _usernameResolverService.GetUsernameAsync(credentialsDto);

            if (userRequestResult.Result is not AuthResultType.Success)
                throw new ArgumentException("Access token was not valid");

            var username = userRequestResult.Message ?? string.Empty;

            var userSubscriptions = await _serverHttpClient.GetUserWebPushSubscriptionsByAccessToken(username);
            await Clients.Caller.SendAsync("ReceiveWebPushSubscriptions", userSubscriptions);
        }

        public async Task RemoveUserWebPushSubscriptions(ClientToServerData data)
        {
            await Clients.All.SendAsync("OnClientToServerDataAck", data.Id);
            var notificationSubscriptionDtOs = await data.Data.DeserializeAsync<NotificationSubscriptionDto[]>();
            await _serverHttpClient.RemoveUserWebPushSubscriptions(notificationSubscriptionDtOs);
            await Clients.Caller.SendAsync("RemovedFromWebPushSubscriptions", notificationSubscriptionDtOs);
            await Clients.Caller.SendAsync("WebPushSubscriptionSetChanged");
        }

        public async Task CheckIfUserExist(ClientToServerData data)
        {
            await Clients.All.SendAsync("OnClientToServerDataAck", data.Id);
            var username = await data.Data.DeserializeAsync<string>();
            IsUserExistDto response = await _serverHttpClient.CheckIfUserExists(username);
            await Clients.Caller.SendAsync("UserExistanceResponse", response);
        }
    }
}