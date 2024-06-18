using System.Collections.Concurrent;

namespace Ethachat.Server.Hubs.UsersConnectedManaging.ConnectedUserStorage
{
    public static class InMemoryHubConnectionStorage
    {
        public static ConcurrentDictionary<string, List<string>> UsersHubConnections { get; set; } = new();
        public static ConcurrentDictionary<string, List<string>> MessageDispatcherHubConnections { get; set; } = new();
    }
}
