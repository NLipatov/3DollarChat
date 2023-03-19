using ClientServerCommon.Models;

namespace Limp.Server.Hubs.UsersConnectedManaging.EventHandling.OnlineUsersRequestEvent
{
    public interface IOnlineUsersManager
    {
        List<UserConnections> GetOnlineUsers();
    }
}
