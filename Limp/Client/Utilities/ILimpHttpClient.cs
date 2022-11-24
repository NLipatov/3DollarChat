using AuthAPI.DTOs.User;
using Limp.Shared.Models.Login;

namespace Limp.Client.Utilities
{
    public interface ILimpHttpClient
    {
        Task<LogInResult> GetJWTPairAsync(UserDTO userDTO);
        Task<string> GetUserNameFromAccessTokenAsync(string accessToken);
    }
}