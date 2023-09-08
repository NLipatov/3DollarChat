using Limp.Client.HubInteraction.Handlers.Helpers;
using Limp.Client.Services.HubConnectionProvider.ConnectionStates;
using Limp.Client.Services.HubServices.HubServices.Implementations.AuthService;
using Limp.Client.Services.HubServices.HubServices.Implementations.MessageService;
using Limp.Client.Services.HubServices.HubServices.Implementations.UsersService;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace Limp.Client.Services.HubConnectionProvider.Implementation
{
    public class HubConnectionProvider : IAsyncDisposable, IHubConnectionProvider
    {
        public HubConnectionProvider
        (IJSRuntime jSRuntime,
        NavigationManager navigationManager,
        IAuthService authService,
        IUsersService usersService,
        IMessageService messageService)
        {
            _jSRuntime = jSRuntime;
            _navigationManager = navigationManager;
            _authService = authService;
            _usersService = usersService;
            _messageService = messageService;
        }
        private readonly IJSRuntime _jSRuntime;
        private readonly NavigationManager _navigationManager;
        private readonly IAuthService _authService;
        private readonly IUsersService _usersService;
        private readonly IMessageService _messageService;
        private HubConnection authHubConnection;
        private HubConnectionProviderState connectionState { get; set; } = HubConnectionProviderState.NotConnected;
        public HubConnectionProviderState GetConnectionState() => connectionState;

        public async Task ConnectToHubs()
        {
            //If user does not have at least one token from JWT pair, ask him to login
            string? accessToken = await JWTHelper.GetAccessTokenAsync(_jSRuntime);
            if (string.IsNullOrWhiteSpace(accessToken)
                ||
                string.IsNullOrWhiteSpace(await JWTHelper.GetRefreshTokenAsync(_jSRuntime)))
            {
                return;
            }

            authHubConnection = await _authService.GetHubConnectionAsync();
            await RefreshTokenIfNeededAsync();
        }

        private async Task RefreshTokenIfNeededAsync()
        {
            await _authService.RenewalAccessTokenIfExpiredAsync(async (isRefreshSucceeded) =>
            {
                if (isRefreshSucceeded)
                {
                    await DecideOnToken();
                }
            });
        }

        private async Task DecideOnToken()
        {
            await _authService.ValidateAccessTokenAsync(async (isTokenValid) =>
            {
                if (isTokenValid)
                {
                    connectionState = HubConnectionProviderState.PartiallyConnected;
                    await ProceedHandle();
                }
            });
        }

        private async Task ProceedHandle()
        {
            await _usersService.GetHubConnectionAsync();
            await _messageService.GetHubConnectionAsync();
            connectionState = HubConnectionProviderState.Connected;
        }

        public async ValueTask DisposeAsync()
        {
            await _messageService.DisconnectAsync();
            await _usersService.DisconnectAsync();
            await _authService.DisconnectAsync();
        }

        public async Task ForceReconnectToHubs()
        {
            await Console.Out.WriteLineAsync("Force reconnecting to hubs...");

            await _messageService.DisconnectAsync();
            await _usersService.DisconnectAsync();
            await _authService.DisconnectAsync();

            await _usersService.GetHubConnectionAsync();
            await _messageService.GetHubConnectionAsync();
            await _authService.GetHubConnectionAsync();
        }
    }
}
