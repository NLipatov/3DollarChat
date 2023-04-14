using Microsoft.AspNetCore.SignalR.Client;

namespace Limp.Client.Services.HubService.AuthService
{
    public interface IAuthService
    {
        Task<HubConnection> ConnectAsync();
        Task DisconnectAsync();
        Task ValidateTokenAsync(Func<bool, Task> callback);
        Task RefreshTokenIfNeededAsync(Func<bool, Task> callback);
    }
}
