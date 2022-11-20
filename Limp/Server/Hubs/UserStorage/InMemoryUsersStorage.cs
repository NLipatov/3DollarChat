using Limp.Shared.Models;

namespace Limp.Server.Hubs.UserStorage
{
    public static class InMemoryUsersStorage
    {
        public static List<UserConnections> UserConnections { get; set; } = new();
    }
}
