using LimpShared.Models.Authentication.Models;
using LimpShared.Models.Authentication.Models.UserAuthentication;
using Microsoft.AspNetCore.SignalR.Client;

namespace Limp.Client.Services.HubService.AuthService
{
    public interface IAuthService
    {
        bool IsConnected();
        Task<HubConnection> ConnectAsync();
        Task DisconnectAsync();
        Task ValidateAccessTokenAsync(Func<bool, Task> isTokenAccessValidCallback);
        Task RenewalAccessTokenIfExpiredAsync(Func<bool, Task> isRenewalSucceededCallback);
        Task LogIn(UserAuthentication userAuthentication);
    }
}
