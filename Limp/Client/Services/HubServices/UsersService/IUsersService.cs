using ClientServerCommon.Models;
using LimpShared.Encryption;
using LimpShared.Models.WebPushNotification;
using Microsoft.AspNetCore.SignalR.Client;

namespace Limp.Client.Services.HubService.UsersService
{
    public interface IUsersService
    {
        Task<HubConnection> ConnectAsync();
        Task ReconnectAsync();
        Task DisconnectAsync();
        Task SetRSAPublicKey(string accessToken, Key RSAPublicKey);
        Task ActualizeConnectedUsersAsync();
        Guid SubscribeToConnectionIdReceived(Func<string, Task> callback);
        void RemoveConnectionIdReceived(Guid subscriptionId);
        Guid SubscribeToUsernameResolved(Func<string, Task> callback);
        void RemoveUsernameResolved(Guid subscriptionId);
        Task CheckIfUserOnline(string username);
        Task SubscribeUserToWebPushNotificationsAsync(NotificationSubscriptionDTO subscriptionDTO);
    }
}
