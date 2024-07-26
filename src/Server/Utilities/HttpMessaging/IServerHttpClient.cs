using EthachatShared.Models.Authentication.Models;
using EthachatShared.Models.Authentication.Models.Credentials.CredentialsDTO;
using EthachatShared.Models.Authentication.Models.UserAuthentication;
using EthachatShared.Models.Users;
using EthachatShared.Models.WebPushNotification;

namespace Ethachat.Server.Utilities.HttpMessaging
{
    public interface IServerHttpClient
    {
        Task<IsUserExistDto> CheckIfUserExists(string username);
        Task<AuthResult> GetJWTPairAsync(UserAuthentication userDTO);
        Task<AuthResult> ValidateCredentials(CredentialsDTO credentials);
        Task<AuthResult> RefreshCredentials(CredentialsDTO credentials);
        Task<AuthResult> Register(UserAuthentication userDTO);
        Task AddUserWebPushSubscribtion(NotificationSubscriptionDto subscriptionDTO);
        Task<NotificationSubscriptionDto[]> GetUserWebPushSubscriptionsByAccessToken(string accessToken);
        Task RemoveUserWebPushSubscriptions(NotificationSubscriptionDto[] subscriptionsToRemove);
        Task<List<AccessRefreshEventLog>> GetTokenRefreshHistory(string accessToken);
        Task<AuthResult> GetUsernameByCredentials(CredentialsDTO credentials);
    }
}