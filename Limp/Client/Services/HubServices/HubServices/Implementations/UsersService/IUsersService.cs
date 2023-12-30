using LimpShared.Encryption;
using LimpShared.Models.Authentication.Models.Credentials.CredentialsDTO;
using LimpShared.Models.WebPushNotification;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.UsersService
{
    public interface IUsersService : IHubService
    {
        Task SetRSAPublicKey(Key RSAPublicKey);
        Task ActualizeConnectedUsersAsync();
        Guid SubscribeToConnectionIdReceived(Func<string, Task> callback);
        void RemoveConnectionIdReceived(Guid subscriptionId);
        Guid SubscribeToUsernameResolved(Func<string, Task> callback);
        void RemoveUsernameResolved(Guid subscriptionId);
        Task CheckIfUserOnline(string username);
        Task AddUserWebPushSubscription(NotificationSubscriptionDto subscriptionDTO);
        Task GetUserWebPushSubscriptions(CredentialsDTO credentialsDto);
        Task RemoveUserWebPushSubscriptions(NotificationSubscriptionDto[] subscriptionsToRemove);
        Task CheckIfUserExists(string username);
    }
}
