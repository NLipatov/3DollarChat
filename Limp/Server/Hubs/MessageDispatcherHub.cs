using Limp.Shared.Models;
using Microsoft.AspNetCore.SignalR;

namespace Limp.Server.Hubs
{
    public class MessageDispatcherHub : Hub
    {
        public async Task Dispatch(string connectionId, Message message)
        {
            await Clients.Client(connectionId).SendAsync("ReceiveMessage", message);
        }
    }
}