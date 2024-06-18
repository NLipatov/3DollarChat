using Ethachat.Server.Hubs.UsersConnectedManaging.ConnectedUserStorage;
using EthachatShared.Models.ConnectedUsersManaging;

namespace Ethachat.Server.Hubs.UsersConnectedManaging.EventHandling.OnlineUsersRequestEvent
{
    public class OnlineUsersManager : IOnlineUsersManager
    {
        public UserConnectionsReport FormUsersOnlineMessage()
        {
            var userHubConnections = InMemoryHubConnectionStorage.UsersHubConnections;
            var messageHubConnections = InMemoryHubConnectionStorage.MessageDispatcherHubConnections;

            var commonConnections = userHubConnections;

            return new()
            {
                FormedAt = DateTime.UtcNow,
                UserConnections = commonConnections.Select(x => new UserConnection
                {
                    Username = x.Key,
                    ConnectionIds = x.Value,
                    UsersHubConnectionIds = userHubConnections.TryGetValue(x.Key, out var usersHubConnectionIds) 
                        ? usersHubConnectionIds 
                        : null,
                    MessageHubConnectionIds = messageHubConnections.TryGetValue(x.Key, out var messageHubConnectionIds) 
                        ? messageHubConnectionIds
                        : null
                }).ToArray()
            };
        }
    }
}
