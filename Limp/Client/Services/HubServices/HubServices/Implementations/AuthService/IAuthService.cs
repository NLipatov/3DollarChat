using System.Collections.Concurrent;
using LimpShared.Models.Authentication.Models.UserAuthentication;

namespace Limp.Client.Services.HubServices.HubServices.Implementations.AuthService
{
    public interface IAuthService : IHubService
    {
        ConcurrentQueue<Func<bool, Task>> IsTokenValidCallbackQueue { get; set; }
        Task ValidateAccessTokenAsync(Func<bool, Task> isTokenAccessValidCallback);
        Task RenewalAccessTokenIfExpiredAsync(Func<bool, Task> isRenewalSucceededCallback);
        Task RenewalCredentialId(Func<bool, Task> isRenewalSucceededCallback);
        Task LogIn(UserAuthentication userAuthentication);
        Task GetRefreshTokenHistory();
        Task GetAuthorisationServerAddress();
    }
}
