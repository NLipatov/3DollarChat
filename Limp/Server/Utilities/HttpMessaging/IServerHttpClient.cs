using AuthAPI.DTOs.User;
using Limp.Client.Utilities.HttpClientUtility.Models;

namespace Limp.Server.Utilities.HttpMessaging
{
    public interface IServerHttpClient
    {
        Task<TokenFetchingResult> GetJWTPairAsync(UserDTO userDTO);
        Task<string> GetUserNameFromAccessTokenAsync(string accessToken);
    }
}