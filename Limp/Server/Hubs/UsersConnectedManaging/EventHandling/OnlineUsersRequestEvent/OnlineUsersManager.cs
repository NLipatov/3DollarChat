using Limp.Server.Hubs.UsersConnectedManaging.ConnectedUserStorage;
using LimpShared.Models.ConnectedUsersManaging;

namespace Limp.Server.Hubs.UsersConnectedManaging.EventHandling.OnlineUsersRequestEvent
{
    public class OnlineUsersManager : IOnlineUsersManager
    {
        public UserConnectionsReport FormUsersOnlineMessage()
        {
            UserConnection[] uConnections = InMemoryHubConnectionStorage.UsersHubConnections
                .Where(x => x.Value.Count > 0)
                .Select(x => new UserConnection
                {
                    Username = x.Key,
                    ConnectionIds = x.Value,
                })
                .ToArray();

            return new() { FormedAt = DateTime.UtcNow, UserConnections = uConnections };
        }
    }
}
