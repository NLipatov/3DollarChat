namespace Limp.Server.Hubs.UserStorage.UserManager;

public class UserConnectionManager : IUserConnectionManager
{
    private Dictionary<string, List<string>> ConnectedUsers = new();
    public List<string> GetConnectedUsers() => ConnectedUsers.Keys.ToList();
    public void HandleConnect(string username, string connectionId)
    {
        lock (ConnectedUsers)
        {
            if (!ConnectedUsers.ContainsKey(username))
                ConnectedUsers[username] = new();
            ConnectedUsers[username].Add(connectionId);
        }
    }

    public void HandleDisconnect(string username, string connectionId)
    {
        lock (ConnectedUsers)
        {
            if (ConnectedUsers.ContainsKey(username))
            {
                ConnectedUsers[username].Remove(connectionId);
                if (ConnectedUsers[username].Count == 0)
                    ConnectedUsers.Remove(username);
            }
        }
    }
}