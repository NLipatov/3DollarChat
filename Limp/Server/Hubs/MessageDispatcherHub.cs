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
            string senderName = message.SenderUsername;

            Clients.Group(senderName).SendAsync("ReceiveMessage", message);
        }

        public override async Task OnConnectedAsync()
        {
            string name = "Anon";

            await Groups.AddToGroupAsync(Context.ConnectionId, name);

            await base.OnConnectedAsync();
        }
    }
}