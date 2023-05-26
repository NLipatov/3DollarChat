using ClientServerCommon.Models.Message;
using Microsoft.AspNetCore.SignalR;

namespace Limp.Server.Hubs.MessageDispatcher.Helpers.MessageSender
{
    public interface IMessageSender
    {
        Task AddAsUnprocessedAsync(Message message, IHubCallerClients clients);
        Task OnMessageReceived(Message message, IHubCallerClients clients);
        Task SendAsync(Message message, IHubCallerClients clients);
    }
}