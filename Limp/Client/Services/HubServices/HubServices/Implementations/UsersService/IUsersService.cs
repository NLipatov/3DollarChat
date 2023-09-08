using LimpShared.Encryption;
using LimpShared.Models.WebPushNotification;

namespace Limp.Client.Services.HubServices.HubServices.Implementations.UsersService
{
    public interface IUsersService : IHubService
    {
        Task ReconnectAsync();
        Task SetRSAPublicKey(string accessToken, Key RSAPublicKey);
        Task ActualizeConnectedUsersAsync();
        Guid SubscribeToConnectionIdReceived(Func<string, Task> callback);
        void RemoveConnectionIdReceived(Guid subscriptionId);
        Guid SubscribeToUsernameResolved(Func<string, Task> callback);
        void RemoveUsernameResolved(Guid subscriptionId);
        Task CheckIfUserOnline(string username);
        Task AddUserWebPushSubscription(NotificationSubscriptionDto subscriptionDTO);
        Task GetUserWebPushSubscriptions(string accessToken);
        Task RemoveUserWebPushSubscriptions(NotificationSubscriptionDto[] subscriptionsToRemove);
        Task CheckIfUserExists(string username);
    }
}
