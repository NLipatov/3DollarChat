using Limp.Server.Hubs.UserStorage;
using Limp.Shared.Models;
using Microsoft.AspNetCore.SignalR;

namespace Limp.Server.Hubs
{
    public class UsersHub : Hub
    {
        public async override Task OnConnectedAsync()
        {
            if (!InMemoryUsersStorage.UserConnections.Any(x => x.ConnectionIds.Contains(Context.ConnectionId)))
            {
                InMemoryUsersStorage.UserConnections.Add(new UserConnections { ConnectionIds = new List<string>() { Context.ConnectionId } });
            }

            //StaticUserStorage.TrackedUserConnections.Add(new TrackedUserConnection { ConnectionId = Context.ConnectionId });
        }

        public async override Task OnDisconnectedAsync(Exception? exception)
        {
            var targetConnectionId = Context.ConnectionId;
            InMemoryUsersStorage.UserConnections.First(x=>x.ConnectionIds.Contains(targetConnectionId)).ConnectionIds.Remove(targetConnectionId);

            InMemoryUsersStorage.UserConnections.RemoveAll(x => x.ConnectionIds.Count == 0);

            await PushOnlineUsersToClients();
        }

        public async Task SetUsername(string username)
        {
            if (InMemoryUsersStorage.UserConnections.Any(x => x.Username == username))
            {
                InMemoryUsersStorage.UserConnections.First(x => x.Username == username).ConnectionIds.Add(Context.ConnectionId);
            }
            else
            {
                InMemoryUsersStorage
                    .UserConnections
                    .First(x => x.ConnectionIds.Contains(Context.ConnectionId)).Username = username;
            }

            await PushOnlineUsersToClients();
            await PushConId();
        }

        public async Task PushOnlineUsersToClients()
        {
            await Clients.All.SendAsync("ReceiveOnlineUsers", InMemoryUsersStorage.UserConnections);
        }

        public async Task PushConId()
        {
            await Clients.Caller.SendAsync("ReceiveConnectionId", Context.ConnectionId);
        }
    }
}