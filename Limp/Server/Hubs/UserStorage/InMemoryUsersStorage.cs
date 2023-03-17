using ClientServerCommon.Models;

namespace Limp.Server.Hubs.UserStorage
{
    public static class InMemoryUsersStorage
    {
        public static List<UserConnections> UsersHubConnections { get; set; } = new();
    }
}
