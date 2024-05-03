using Ethachat.Server.Hubs.UsersConnectedManaging.ConnectedUserStorage;
using Ethachat.Server.Hubs.UsersConnectedManaging.EventHandling;
using Ethachat.Server.Hubs.UsersConnectedManaging.EventHandling.OnlineUsersRequestEvent;
using Ethachat.Server.Services.LogService;
using Ethachat.Server.Utilities.HttpMessaging;
using Ethachat.Server.Utilities.UsernameResolver;
using EthachatShared.Encryption;
using EthachatShared.Models.Authentication.Models;
using EthachatShared.Models.Authentication.Models.AuthenticatedUserRepresentation.PublicKey;
using EthachatShared.Models.Authentication.Models.Credentials.CredentialsDTO;
using EthachatShared.Models.Authentication.Models.Credentials.Implementation;
using EthachatShared.Models.Authentication.Types;
using EthachatShared.Models.ConnectedUsersManaging;
using EthachatShared.Models.Users;
using EthachatShared.Models.WebPushNotification;
using Microsoft.AspNetCore.SignalR;
using LogLevel = EthachatShared.Models.Logging.ExceptionLogging.LogLevel;

namespace Ethachat.Server.Hubs
{
    public class UsersHub : Hub
    {
        private readonly IServerHttpClient _serverHttpClient;
        private readonly IUserConnectedHandler<UsersHub> _userConnectedHandler;
        private readonly IOnlineUsersManager _onlineUsersManager;
        private readonly IUsernameResolverService _usernameResolverService;
        private readonly ILogService _logService;

        public UsersHub
        (IServerHttpClient serverHttpClient,
            IUserConnectedHandler<UsersHub> userConnectedHandler,
            IOnlineUsersManager onlineUsersManager,
            IUsernameResolverService usernameResolverService,
            ILogService logService)
        {
            _serverHttpClient = serverHttpClient;
            _userConnectedHandler = userConnectedHandler;
            _onlineUsersManager = onlineUsersManager;
            _usernameResolverService = usernameResolverService;
            _logService = logService;
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

                await PushOnlineUsersToClients();
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

                await PushOnlineUsersToClients();
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

                await _serverHttpClient.PostAnRSAPublic(new PublicKeyDto
                {
                    Key = RSAPublicKey.Value!.ToString(),
                    Username = username,
                    AuthenticationType = authenticationType,
                });
            }
            catch (Exception e)
            {
                await _logService.LogAsync(e);
                throw;
            }

            await PushOnlineUsersToClients();
        }

        private async Task OnUsernameResolvedHandlers(string username)
        {
            await PushOnlineUsersToClients();
            await PushConId();
            await PushResolvedName(username);
        }

        private async Task PushResolvedName(string username)
        {
            await Clients.Caller.SendAsync("OnNameResolve", username);
        }

        public async Task PushOnlineUsersToClients()
        {
            //Defines a set of clients that are connected to both UsersHub and MessageDispatcherHub at the same time
            UserConnectionsReport report = _onlineUsersManager.FormUsersOnlineMessage();
            //Pushes set of clients to all the clients
            await Clients.All.SendAsync("ReceiveOnlineUsers", report);
        }

        public async Task PushConId()
        {
            await Clients.Caller.SendAsync("ReceiveConnectionId", Context.ConnectionId);
        }

        public async Task PostAnRSAPublic(PublicKeyDto publicKeyDTO)
        {
            await _serverHttpClient.PostAnRSAPublic(publicKeyDTO);
        }

        public async Task IsUserOnline(string username)
        {
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

        public async Task AddUserWebPushSubscription(NotificationSubscriptionDto notificationSubscriptionDTO)
        {
            await _serverHttpClient.AddUserWebPushSubscribtion(notificationSubscriptionDTO);
            await Clients.Caller.SendAsync("WebPushSubscriptionSetChanged");
        }

        public async Task GetUserWebPushSubscriptions(CredentialsDTO credentialsDto)
        {
            var userRequestResult = await _usernameResolverService.GetUsernameAsync(credentialsDto);

            if (userRequestResult.Result is not AuthResultType.Success)
                throw new ArgumentException("Access token was not valid");

            var username = userRequestResult.Message ?? string.Empty;

            var userSubscriptions = await _serverHttpClient.GetUserWebPushSubscriptionsByAccessToken(username);
            await Clients.Caller.SendAsync("ReceiveWebPushSubscriptions", userSubscriptions);
        }

        public async Task RemoveUserWebPushSubscriptions(NotificationSubscriptionDto[] notificationSubscriptionDTOs)
        {
            await _serverHttpClient.RemoveUserWebPushSubscriptions(notificationSubscriptionDTOs);
            await Clients.Caller.SendAsync("RemovedFromWebPushSubscriptions", notificationSubscriptionDTOs);
            await Clients.Caller.SendAsync("WebPushSubscriptionSetChanged");
        }

        public async Task CheckIfUserExist(string username)
        {
            IsUserExistDto response = await _serverHttpClient.CheckIfUserExists(username);
            await Clients.Caller.SendAsync("UserExistanceResponse", response);
        }
    }
}