using ClientServerCommon.Models.Login;
using Limp.Client.HubInteraction.EventSubscriptionManager;
using Limp.Client.HubInteraction.Handlers.Helpers;
using Limp.Client.Utilities;
using LimpShared.Authentification;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace Limp.Client.HubInteraction.Handlers
{
    public class AuthHandler : IHandler<AuthHandler>
    {
        private readonly NavigationManager _navigationManager;
        private readonly IJSRuntime _jSRuntime;
        private HubConnection? authHub;
        public AuthHandler(NavigationManager navigationManager,
        IJSRuntime jSRuntime)
        {
            _navigationManager = navigationManager;
            _jSRuntime = jSRuntime;
        }
        public async Task<HubConnection> ConnectAsync()
        {
            authHub = new HubConnectionBuilder()
            .WithUrl(_navigationManager.ToAbsoluteUri("/authHub"))
            .Build();

            authHub.On<AuthResult>("OnTokensRefresh", async result =>
            {
                AuthHubSubscriptionManager.CallJWTPairRefresh(result);
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

        public void Dispose()
        {
            AuthHubSubscriptionManager.UnsubscriveAll();
        }
    }
}
