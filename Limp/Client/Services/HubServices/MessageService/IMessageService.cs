using LimpShared.Models.Message;
using Microsoft.AspNetCore.SignalR.Client;

namespace Limp.Client.Services.HubServices.MessageService
{
    public interface IMessageService
    {
        Task<HubConnection> ConnectAsync();
        Task DisconnectAsync();
        Task ReconnectAsync();
        bool IsConnected();
        Task SendMessage(Message message);
        Task NegotiateOnAESAsync(string partnerUsername); 
        Task SendUserMessage(string text, string targetGroup, string myUsername);
        Task NotifySenderThatMessageWasReaded(Guid messageId, string messageSender, string myUsername);
    }
}
