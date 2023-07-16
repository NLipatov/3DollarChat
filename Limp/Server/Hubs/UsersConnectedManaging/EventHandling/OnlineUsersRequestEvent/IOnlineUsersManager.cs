using ClientServerCommon.Models;
using LimpShared.Models.ConnectedUsersManaging;

namespace Limp.Server.Hubs.UsersConnectedManaging.EventHandling.OnlineUsersRequestEvent
{
    public interface IOnlineUsersManager
    {
        UserConnectionsReport FormUsersOnlineMessage();
    }
}
