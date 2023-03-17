using ClientServerCommon.Models;

namespace Limp.Server.Hubs.UserStorage
{
    public static class InMemoryHubConnectionStorage
    {
        public static List<UserConnections> UsersHubConnections { get; set; } = new();
        public static List<UserConnections> MessageDispatcherHubConnections { get; set; } = new();
    }
}
