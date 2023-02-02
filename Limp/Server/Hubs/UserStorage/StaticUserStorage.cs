using ClientServerCommon.Models;

namespace Limp.Server.Hubs.UserStorage
{
    public static class StaticUserStorage
    {
        public static List<TrackedUserConnection> TrackedUserConnections { get; set; } = new();
    }
}
