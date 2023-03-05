using AuthAPI.DTOs.User;
using ClientServerCommon.Models.Login;
using LimpShared.Authentification;

namespace Limp.Server.Utilities.HttpMessaging
{
    public interface IServerHttpClient
    {
        Task<AuthResult> ExplicitJWTPairRefresh(RefreshToken refreshToken);
        Task<AuthResult> GetJWTPairAsync(UserDTO userDTO);
        Task<TokenRelatedOperationResult> GetUserNameFromAccessTokenAsync(string accessToken);
        Task<bool> IsAccessTokenValid(string accessToken);
        Task<AuthResult> Register(UserDTO userDTO);
        Task PostAnRSAPublic(string pemEncodedRSAPublicKey, string username);
        Task<string?> GetAnRSAPublicKey(string username);
    }
}