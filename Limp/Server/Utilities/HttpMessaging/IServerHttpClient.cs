using EthachatShared.Models.Authentication.Models;
using EthachatShared.Models.Authentication.Models.AuthenticatedUserRepresentation.PublicKey;
using EthachatShared.Models.Authentication.Models.Credentials.CredentialsDTO;
using EthachatShared.Models.Authentication.Models.UserAuthentication;
using EthachatShared.Models.Users;
using EthachatShared.Models.WebPushNotification;

namespace Limp.Server.Utilities.HttpMessaging
{
    public interface IServerHttpClient
    {
        Task<IsUserExistDto> CheckIfUserExists(string username);
        Task<AuthResult> GetJWTPairAsync(UserAuthentication userDTO);
        Task<TokenRelatedOperationResult> GetUserNameFromAccessTokenAsync(string accessToken);
        Task<AuthResult> ValidateCredentials(CredentialsDTO credentials);
        Task<AuthResult> RefreshCredentials(CredentialsDTO credentials);
        Task<AuthResult> Register(UserAuthentication userDTO);
        Task PostAnRSAPublic(PublicKeyDto publicKeyDTO);
        Task<string?> GetAnRSAPublicKey(string username);
        Task AddUserWebPushSubscribtion(NotificationSubscriptionDto subscriptionDTO);
        Task<NotificationSubscriptionDto[]> GetUserWebPushSubscriptionsByAccessToken(string accessToken);
        Task RemoveUserWebPushSubscriptions(NotificationSubscriptionDto[] subscriptionsToRemove);
        Task<List<AccessRefreshEventLog>> GetTokenRefreshHistory(string accessToken);
        Task<string> GetServerAddress();
        Task<AuthResult> GetUsernameByCredentials(CredentialsDTO credentials);
    }
}