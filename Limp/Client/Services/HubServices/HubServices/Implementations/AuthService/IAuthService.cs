using LimpShared.Models.Authentication.Models.UserAuthentication;

namespace Limp.Client.Services.HubServices.HubServices.Implementations.AuthService
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
