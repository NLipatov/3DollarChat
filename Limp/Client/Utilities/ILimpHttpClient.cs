using AuthAPI.DTOs.User;
using LimpShared.Authentification;

namespace Limp.Client.Utilities
{
    public interface ILimpHttpClient
    {
        Task<JWTPair?> GetJWTPairAsync(UserDTO userDTO);
        Task<string> GetUserNameFromAccessTokenAsync(string accessToken);
    }
}