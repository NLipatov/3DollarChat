using Limp.Server.Hubs.UsersConnectedManaging.ConnectedUserStorage;
using LimpShared.Models.ConnectedUsersManaging;

namespace Limp.Server.Hubs.UsersConnectedManaging.EventHandling.OnlineUsersRequestEvent
{
    public class OnlineUsersManager : IOnlineUsersManager
    {
        public UserConnectionsReport FormUsersOnlineMessage()
        {
            var userHubConnections = InMemoryHubConnectionStorage.UsersHubConnections;
            var messageHubConnections = InMemoryHubConnectionStorage.MessageDispatcherHubConnections;

            //var commonConnections = userHubConnections.Where(x => messageHubConnections.Any(m => m.Key == x.Key));
            var commonConnections = userHubConnections;

            Console.WriteLine("Pushing as online:");
            foreach (var key in commonConnections.Select(x => x.Key))
            {
                Console.WriteLine(key);
            }
            Console.WriteLine();

            return new()
            {
                FormedAt = DateTime.UtcNow,
                UserConnections = commonConnections.Select(x => new UserConnection
                {
                    Username = x.Key,
                    ConnectionIds = x.Value,
                    UsersHubConnectionIds = userHubConnections[x.Key],
                    //MessageHubConnectionIds = messageHubConnections[x.Key]
                }).ToArray()
            };
        }
    }
}
