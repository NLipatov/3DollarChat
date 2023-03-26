using ClientServerCommon.Models.Login;
using Limp.Client.HubInteraction.Handlers.Helpers;
using Limp.Client.HubInteraction.HubObservers;
using Limp.Client.HubInteraction.HubObservers.Implementations.AuthHub.EventTypes;
using Limp.Client.Utilities;
using LimpShared.Authentification;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace Limp.Client.HubConnectionManagement.ConnectionHandlers.HubInteraction.Implementations
{
    public class AuthHubInteractor : IHubInteractor<AuthHubInteractor>
    {
        private readonly NavigationManager _navigationManager;
        private readonly IJSRuntime _jSRuntime;
        private readonly IHubObserver<AuthHubEvent> _authHubObserver;
        private HubConnection? authHub;
        public AuthHubInteractor
        (NavigationManager navigationManager,
        IJSRuntime jSRuntime,
        IHubObserver<AuthHubEvent> authHubObserver)
        {
            _navigationManager = navigationManager;
            _jSRuntime = jSRuntime;
            _authHubObserver = authHubObserver;
        }
        public async Task<HubConnection> ConnectAsync()
        {
            authHub = new HubConnectionBuilder()
            .WithUrl(_navigationManager.ToAbsoluteUri("/authHub"))
            .Build();

            authHub.On<AuthResult>("OnTokensRefresh", async result =>
            {
                JWTPair? pair = result.JWTPair;
                if (pair == null || string.IsNullOrWhiteSpace(pair.AccessToken) || string.IsNullOrWhiteSpace(pair.RefreshToken.Token))
                    _navigationManager.NavigateTo("/login");

                if (result.Result == AuthResultType.Success)
                {
                    await _jSRuntime.InvokeVoidAsync("localStorage.setItem", "access-token", pair!.AccessToken);
                    await _jSRuntime.InvokeVoidAsync("localStorage.setItem", "refresh-token", pair!.RefreshToken.Token);
                }
            });

            await authHub.StartAsync();

            if (TokenReader.HasAccessTokenExpired(await JWTHelper.GetAccessToken(_jSRuntime)))
            {
                await authHub.SendAsync("RefreshTokens", new RefreshToken
                {
                    Token = await JWTHelper.GetRefreshToken(_jSRuntime)
                });
            }

            return authHub;
        }

        public async ValueTask DisposeAsync()
        {
            _authHubObserver.UnsubscriveAll();
        }
    }
}
