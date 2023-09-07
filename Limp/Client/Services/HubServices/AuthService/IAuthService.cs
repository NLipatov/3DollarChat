using Limp.Client.Services.HubServices.HubServiceContract;
using LimpShared.Models.Authentication.Models.UserAuthentication;

namespace Limp.Client.Services.HubService.AuthService
{
    public interface IAuthService : IHubService
    {
        bool IsConnected();
        Task ValidateAccessTokenAsync(Func<bool, Task> isTokenAccessValidCallback);
        Task RenewalAccessTokenIfExpiredAsync(Func<bool, Task> isRenewalSucceededCallback);
        Task LogIn(UserAuthentication userAuthentication);
        Task GetRefreshTokenHistory();
    }
}
