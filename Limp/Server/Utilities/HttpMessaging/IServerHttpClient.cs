using AuthAPI.DTOs.User;
using Limp.Shared.Models.Login;
using LimpShared.Authentification;

namespace Limp.Server.Utilities.HttpMessaging
{
    public interface IServerHttpClient
    {
        Task<LogInResult> ExplicitJWTPairRefresh(RefreshToken refreshToken);
        Task<LogInResult> GetJWTPairAsync(UserDTO userDTO);
        Task<TokenRelatedOperationResult> GetUserNameFromAccessTokenAsync(string accessToken);
        Task<bool> IsAccessTokenValid(string accessToken);
    }
}