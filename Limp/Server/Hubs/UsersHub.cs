using Limp.Server.Hubs.UserStorage;
using Limp.Shared.Models;
using Microsoft.AspNetCore.SignalR;

namespace Limp.Server.Hubs
{
    public class UsersHub : Hub
    {
        public async override Task OnConnectedAsync()
        {
            StaticUserStorage.TrackedUserConnections.Add(new TrackedUserConnection { ConnectionId = Context.ConnectionId });
            await PushOnlineUsersToClients();
            await PushConId();
        }

        public async override Task OnDisconnectedAsync(Exception? ex)
        {
            TrackedUserConnection disconnectedUser = StaticUserStorage.TrackedUserConnections.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
            StaticUserStorage.TrackedUserConnections.Remove(disconnectedUser);
            await PushOnlineUsersToClients();
        }

        public void AssociateWithConnectionId(string username)
        {
            var connectionId = Context.ConnectionId;
            StaticUserStorage.TrackedUserConnections.First(x => x.ConnectionId == connectionId).Username = username;
        }

        public async Task PushOnlineUsersToClients()
        {
            await Clients.All.SendAsync("ReceiveOnlineUsers", StaticUserStorage.TrackedUserConnections);
        }

        public async Task PushConId()
        {
            await Clients.Caller.SendAsync("ReceiveConnectionId", Context.ConnectionId);
        }
    }
}