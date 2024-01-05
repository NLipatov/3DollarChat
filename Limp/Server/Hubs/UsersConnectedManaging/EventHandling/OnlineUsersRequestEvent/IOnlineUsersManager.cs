using EthachatShared.Models.ConnectedUsersManaging;

namespace Ethachat.Server.Hubs.UsersConnectedManaging.EventHandling.OnlineUsersRequestEvent
{
    public interface IOnlineUsersManager
    {
        UserConnectionsReport FormUsersOnlineMessage();
    }
}
