using AuthAPI.DTOs.User;
using LimpShared.Models.Login;

namespace Limp.Client.Utilities
{
    public interface ILimpHttpClient
    {
        Task<TokenFetchingResult> GetJWTPairAsync(UserDTO userDTO);
        Task<string> GetUserNameFromAccessTokenAsync(string accessToken);
    }
}