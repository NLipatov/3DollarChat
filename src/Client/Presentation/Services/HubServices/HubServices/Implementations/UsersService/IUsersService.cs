using EthachatShared.Models.Authentication.Models.Credentials.CredentialsDTO;
using EthachatShared.Models.WebPushNotification;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.UsersService
{
    public interface IUsersService : IHubService
    {
        Task ActualizeConnectedUsersAsync();
        Task CheckIfUserOnline(string username);
        Task AddUserWebPushSubscription(NotificationSubscriptionDto subscriptionDTO);
        Task GetUserWebPushSubscriptions(CredentialsDTO credentialsDto);
        Task RemoveUserWebPushSubscriptions(NotificationSubscriptionDto[] subscriptionsToRemove);
        Task CheckIfUserExists(string username);
        void PreventReconnecting();
        Task ReconnectAsync();
    }
}
