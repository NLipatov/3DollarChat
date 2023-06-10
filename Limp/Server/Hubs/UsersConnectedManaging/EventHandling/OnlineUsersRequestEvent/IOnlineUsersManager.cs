using ClientServerCommon.Models;
using ClientServerCommon.Models.HubMessages;

namespace Limp.Server.Hubs.UsersConnectedManaging.EventHandling.OnlineUsersRequestEvent
{
    public interface IOnlineUsersManager
    {
        UsersOnlineMessage FormUsersOnlineMessage();
    }
}
