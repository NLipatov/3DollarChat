using LimpShared.Models.Authentication.Models;
using LimpShared.Models.Authentication.Models.AuthenticatedUserRepresentation.PublicKey;
using LimpShared.Models.Authentication.Models.UserAuthentication;
using LimpShared.Models.Users;
using LimpShared.Models.WebPushNotification;

namespace Limp.Server.Utilities.HttpMessaging
{
    public interface IServerHttpClient
    {
        Task<IsUserExistDTO> CheckIfUserExists(string username);
        Task<AuthResult> ExplicitJWTPairRefresh(RefreshToken refreshToken);
        Task<AuthResult> GetJWTPairAsync(UserAuthentication userDTO);
        Task<TokenRelatedOperationResult> GetUserNameFromAccessTokenAsync(string accessToken);
        Task<bool> IsAccessTokenValid(string accessToken);
        Task<AuthResult> Register(UserAuthentication userDTO);
        Task PostAnRSAPublic(PublicKeyDTO publicKeyDTO);
        Task<string?> GetAnRSAPublicKey(string username);
        Task AddUserWebPushSubscribtion(NotificationSubscriptionDTO subscriptionDTO);
        Task<NotificationSubscriptionDTO[]> GetUserWebPushSubscriptionsByAccessToken(string accessToken);
        Task RemoveUserWebPushSubscriptions(NotificationSubscriptionDTO[] subscriptionsToRemove);
    }
}