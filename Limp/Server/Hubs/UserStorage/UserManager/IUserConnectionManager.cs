using ClientServerCommon.Models;
using System.Collections.Concurrent;

namespace Limp.Server.Hubs.UserStorage.UserManager;

public interface IUserConnectionManager
{
    void HandleConnect(string username, string connectionId);
    void HandleDisconnect(string username, string connectionId);
    List<string> GetConnectedUsers();
}
