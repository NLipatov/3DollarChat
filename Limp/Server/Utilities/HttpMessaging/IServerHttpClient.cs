using AuthAPI.DTOs.User;
using Limp.Shared.Models.Login;

namespace Limp.Server.Utilities.HttpMessaging
{
    public interface IServerHttpClient
    {
        Task<LogInResult> GetJWTPairAsync(UserDTO userDTO);
        Task<string> GetUserNameFromAccessTokenAsync(string accessToken);
    }
}