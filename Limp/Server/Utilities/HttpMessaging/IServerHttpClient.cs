using LimpShared.Models.Authentication.Models;
using LimpShared.Models.Authentication.Models.AuthenticatedUserRepresentation.PublicKey;
using LimpShared.Models.Authentication.Models.UserAuthentication;
using LimpShared.Models.Users;
using LimpShared.Models.WebPushNotification;

namespace Limp.Server.Utilities.HttpMessaging
{
    public interface IServerHttpClient
    {
        Task<IsUserExistDto> CheckIfUserExists(string username);
        Task<AuthResult> ExplicitJWTPairRefresh(RefreshTokenDto refreshToken);
        Task<AuthResult> GetJWTPairAsync(UserAuthentication userDTO);
        Task<TokenRelatedOperationResult> GetUserNameFromAccessTokenAsync(string accessToken);
        Task<bool> IsAccessTokenValid(string accessToken);
        Task<AuthResult> Register(UserAuthentication userDTO);
        Task PostAnRSAPublic(PublicKeyDto publicKeyDTO);
        Task<string?> GetAnRSAPublicKey(string username);
        Task AddUserWebPushSubscribtion(NotificationSubscriptionDto subscriptionDTO);
        Task<NotificationSubscriptionDto[]> GetUserWebPushSubscriptionsByAccessToken(string accessToken);
        Task RemoveUserWebPushSubscriptions(NotificationSubscriptionDto[] subscriptionsToRemove);
        Task<List<AccessRefreshEventLog>> GetTokenRefreshHistory(string accessToken);
        Task<string> GetServerAddress();
    }
}