using AuthAPI.DTOs.User;
using ClientServerCommon.Models.Login;
using LimpShared.Authentification;
using LimpShared.DTOs.PublicKey;

namespace Limp.Server.Utilities.HttpMessaging
{
    public interface IServerHttpClient
    {
        Task<AuthResult> ExplicitJWTPairRefresh(RefreshToken refreshToken);
        Task<AuthResult> GetJWTPairAsync(UserDTO userDTO);
        Task<TokenRelatedOperationResult> GetUserNameFromAccessTokenAsync(string accessToken);
        Task<bool> IsAccessTokenValid(string accessToken);
        Task<AuthResult> Register(UserDTO userDTO);
        Task PostAnRSAPublic(PublicKeyDTO publicKeyDTO);
        Task<string?> GetAnRSAPublicKey(string username);
    }
}