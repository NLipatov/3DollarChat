using LimpShared.Models.Authentication.Models;
using LimpShared.Models.Authentication.Models.AuthenticatedUserRepresentation.PublicKey;
using LimpShared.Models.Authentication.Models.UserAuthentication;

namespace Limp.Server.Utilities.HttpMessaging
{
    public interface IServerHttpClient
    {
        Task<AuthResult> ExplicitJWTPairRefresh(RefreshToken refreshToken);
        Task<AuthResult> GetJWTPairAsync(UserAuthentication userDTO);
        Task<TokenRelatedOperationResult> GetUserNameFromAccessTokenAsync(string accessToken);
        Task<bool> IsAccessTokenValid(string accessToken);
        Task<AuthResult> Register(UserAuthentication userDTO);
        Task PostAnRSAPublic(PublicKeyDTO publicKeyDTO);
        Task<string?> GetAnRSAPublicKey(string username);
    }
}