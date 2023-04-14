using ClientServerCommon.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace Limp.Client.Services.HubService.UsersService
{
    public interface IUsersService
    {
        Task<HubConnection> ConnectAsync();
        Task DisconnectAsync();
        Guid SubscribeToUsersOnlineUpdate(Func<List<UserConnection>, Task> callback);
        void RemoveSubsctiptionToUsersOnlineUpdate(Guid subscriptionId);
        Guid SubscribeToConnectionIdReceived(Func<string, Task> callback);
        void RemoveConnectionIdReceived(Guid subscriptionId);
        Guid SubscribeToUsernameResolved(Func<string, Task> callback);
        void RemoveUsernameResolved(Guid subscriptionId);
    }
}
