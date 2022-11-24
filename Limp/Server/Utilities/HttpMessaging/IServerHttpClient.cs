using AuthAPI.DTOs.User;
using LimpShared.Models.Login;

namespace Limp.Server.Utilities.HttpMessaging
{
    public interface IServerHttpClient
    {
        Task<TokenFetchingResult> GetJWTPairAsync(UserDTO userDTO);
        Task<string> GetUserNameFromAccessTokenAsync(string accessToken);
    }
}