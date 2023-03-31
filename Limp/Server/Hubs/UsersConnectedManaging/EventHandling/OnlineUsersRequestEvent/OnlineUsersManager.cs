using ClientServerCommon.Models;
using Limp.Server.Hubs.UsersConnectedManaging.ConnectedUserStorage;

namespace Limp.Server.Hubs.UsersConnectedManaging.EventHandling.OnlineUsersRequestEvent
{
    public class OnlineUsersManager : IOnlineUsersManager
    {
        public List<UserConnection> GetOnlineUsers()
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

            List<UserConnection> commonConnections = uConnections.Where(u=>mdConnections.Any(md=>md.Username == u.Username)).ToList();

            return commonConnections;
        }
    }
}
