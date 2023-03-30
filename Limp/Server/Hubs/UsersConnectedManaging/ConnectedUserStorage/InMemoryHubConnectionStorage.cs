using ClientServerCommon.Models;

namespace Limp.Server.Hubs.UsersConnectedManaging.ConnectedUserStorage
{
    public static class InMemoryHubConnectionStorage
    {
        public static List<UserConnection> UsersHubConnections { get; set; } = new();
        public static List<UserConnection> MessageDispatcherHubConnections { get; set; } = new();
    }
}
