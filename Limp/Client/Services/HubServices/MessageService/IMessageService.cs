using ClientServerCommon.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace Limp.Client.Services.HubServices.MessageService
{
    public interface IMessageService
    {
        Task<HubConnection> ConnectAsync();
        Task DisconnectAsync();
        Task ReconnectAsync();
        Task RequestForPartnerPublicKey(string partnerUsername);
        Guid SubscribeToUsersOnline(Func<List<UserConnection>, Task> callback);
        void RemoveSubscriptionToUsersOnline(Guid subscriptionId);
        Guid SubscribeToPartnerAESAccept(Func<string, Task> callback);
        void RemoveSubscriptionToPartnerAESAccept(Guid subscriptionId);
    }
}
