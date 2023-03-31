using ClientServerCommon.Models;
using System.Collections.Concurrent;

namespace Limp.Server.Hubs.UsersConnectedManaging.ConnectedUserStorage
{
    public static class InMemoryHubConnectionStorage
    {
        public static ConcurrentDictionary<string, List<string>> UsersHubConnections { get; set; } = new();
        public static List<UserConnection> MessageDispatcherHubConnections { get; set; } = new();
    }
}
