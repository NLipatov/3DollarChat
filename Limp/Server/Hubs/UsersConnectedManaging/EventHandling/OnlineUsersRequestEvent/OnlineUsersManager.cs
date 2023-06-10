using ClientServerCommon.Models;
using ClientServerCommon.Models.HubMessages;
using Limp.Server.Hubs.UsersConnectedManaging.ConnectedUserStorage;

namespace Limp.Server.Hubs.UsersConnectedManaging.EventHandling.OnlineUsersRequestEvent
{
    public class OnlineUsersManager : IOnlineUsersManager
    {
        public UsersOnlineMessage FormUsersOnlineMessage()
        {
            List<UserConnection> mdConnections = InMemoryHubConnectionStorage.MessageDispatcherHubConnections
                .Where(x => x.Value.Count > 0)
                .Select(x => new UserConnection
                {
                    Username = x.Key,
                    ConnectionIds = x.Value,
                })
                .ToList();

            List<UserConnection> uConnections = InMemoryHubConnectionStorage.UsersHubConnections
                .Where(x => x.Value.Count > 0)
                .Select(x => new UserConnection
                {
                    Username = x.Key,
                    ConnectionIds = x.Value,
                })
                .ToList();

            UserConnection[] commonConnections = uConnections.Where(u => mdConnections.Any(md => md.Username == u.Username)).ToArray();

            return new() { FormedAt = DateTime.UtcNow, UserConnections = commonConnections };
        }
    }
}
