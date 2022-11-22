using Limp.Server.Hubs.UserStorage;
using Limp.Shared.Models;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace Limp.Server.Hubs
{
    public class MessageDispatcherHub : Hub
    {
        public void Dispatch(Message message)
        {
            string targetGroup = message.TargetGroup;

            Clients.Group(targetGroup).SendAsync("ReceiveMessage", message);
        }

        public async Task SetUsername(string username)
        {
            if (InMemoryUsersStorage.UserConnections.Any(x => x.Username == username))
            {
                InMemoryUsersStorage.UserConnections.First(x => x.Username == username).ConnectionIds.Add(Context.ConnectionId);

                foreach (var connection in InMemoryUsersStorage.UserConnections.First(x => x.Username == username).ConnectionIds)
                {
                    await Groups.AddToGroupAsync(connection, username);
                }
            }
            else
            {
                InMemoryUsersStorage
                    .UserConnections
                    .First(x => x.ConnectionIds.Contains(Context.ConnectionId)).Username = username;
            }
        }
    }
}