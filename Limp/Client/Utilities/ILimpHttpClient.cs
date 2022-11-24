using AuthAPI.DTOs.User;
using Limp.Client.Utilities.HttpClientUtility.Models;
using LimpShared.Authentification;

namespace Limp.Client.Utilities
{
    public interface ILimpHttpClient
    {
        Task<TokenFetchingResult> GetJWTPairAsync(UserDTO userDTO);
        Task<string> GetUserNameFromAccessTokenAsync(string accessToken);
    }
}