using ClientServerCommon.Models;

namespace Limp.Client.HubInteraction.EventSubscriptionManager
{
    public static class UsersHubSubscriptionManager
    {
        public static List<Action<string>> OnConnectionIdReceived { get; set; } = new();
        public static List<Action<List<UserConnections>>> OnUsersConnectionsReceived { get; set; } = new();
        public static List<Action<string>> OnUsernameResolved { get; set; } = new();

        public static void SubscribeToConnectionIdReceived(Action<string> callback)
        {
            OnConnectionIdReceived.Add(callback);
        }
        public static void SubscribeToUsersConnectionsReceived(Action<List<UserConnections>> callback)
        {
            OnUsersConnectionsReceived.Add(callback);
        }
        public static void SubscribeToUsernameResolved(Action<string> callback)
        {
            OnUsernameResolved.Add(callback);
        }
        public static void CallConnectionIdReceived(string connectionId)
        {
            foreach (var method in OnConnectionIdReceived)
            {
                method(connectionId);
            }
        }
        public static void CallUsersConnectionsReceived(List<UserConnections> usersConnections)
        {
            foreach(var method in OnUsersConnectionsReceived) 
            {
                method(usersConnections);
            }
        }
        public static void CallUsernameResolved(string username)
        {
            foreach (var method in OnUsernameResolved)
            {
                method(username);
            }
        }
    }
}
