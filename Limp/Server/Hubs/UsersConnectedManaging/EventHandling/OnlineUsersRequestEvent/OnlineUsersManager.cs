using Limp.Server.Hubs.UsersConnectedManaging.ConnectedUserStorage;
using LimpShared.Models.ConnectedUsersManaging;

namespace Limp.Server.Hubs.UsersConnectedManaging.EventHandling.OnlineUsersRequestEvent
{
    public class OnlineUsersManager : IOnlineUsersManager
    {
        public UserConnectionsReport FormUsersOnlineMessage()
        {
            return new() 
            {
                FormedAt = DateTime.UtcNow, 
                UserConnections = InMemoryHubConnectionStorage.UserConnections.ToArray() 
            };
        }
    }
}
