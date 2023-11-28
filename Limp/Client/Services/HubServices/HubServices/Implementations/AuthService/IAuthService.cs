using System.Collections.Concurrent;
using LimpShared.Models.Authentication.Models;
using LimpShared.Models.Authentication.Models.UserAuthentication;

namespace Limp.Client.Services.HubServices.HubServices.Implementations.AuthService
{
    public interface IAuthService : IHubService
    {
        ConcurrentQueue<Func<AuthResult, Task>> IsTokenValidCallbackQueue { get; set; }
        Task ValidateAccessTokenAsync(Func<AuthResult, Task> isTokenAccessValidCallback);
        Task LogIn(UserAuthentication userAuthentication);
        Task GetRefreshTokenHistory();
        Task GetAuthorisationServerAddress();
    }
}
