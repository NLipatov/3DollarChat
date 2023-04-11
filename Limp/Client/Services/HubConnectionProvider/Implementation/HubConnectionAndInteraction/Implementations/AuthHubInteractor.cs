using ClientServerCommon.Models.Login;
using Limp.Client.HubInteraction.Handlers.Helpers;
using Limp.Client.HubInteraction.HubObservers;
using Limp.Client.HubInteraction.HubObservers.Implementations.AuthHub.EventTypes;
using LimpShared.Authentification;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace Limp.Client.Services.HubConnectionProvider.Implementation.HubInteraction.Implementations
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
                if (result.Result == AuthResultType.Success)
                {
                    JWTPair? jwtPair = result.JWTPair;
                    await _jSRuntime.InvokeVoidAsync("localStorage.setItem", "access-token", jwtPair!.AccessToken);
                    await _jSRuntime.InvokeVoidAsync("localStorage.setItem", "refresh-token", jwtPair!.RefreshToken.Token);
                }
                else
                {
                    _navigationManager.NavigateTo("/login");
                }
            });

            await authHub.StartAsync();

            await RefreshTokenPairIfExpired();

            return authHub;
        }

        private async Task RefreshTokenPairIfExpired()
        {
            if (TokenReader.HasAccessTokenExpired((await JWTHelper.GetAccessToken(_jSRuntime))!))
            {
                await authHub!.SendAsync("RefreshTokens", new RefreshToken
                {
                    Token = (await JWTHelper.GetRefreshToken(_jSRuntime))!
                });
            }
        }

        public async ValueTask DisposeAsync()
        {
            _authHubObserver.UnsubscriveAll();
            if (authHub != null)
            {
                await authHub.StopAsync();
                await authHub.DisposeAsync();
            }
        }
    }
}
