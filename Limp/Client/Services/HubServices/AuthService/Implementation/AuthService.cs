using ClientServerCommon.Models.Login;
using Limp.Client.HubInteraction.Handlers.Helpers;
using Limp.Client.Services.HubService.CommonServices;
using LimpShared.Authentification;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using System.Collections.Concurrent;

namespace Limp.Client.Services.HubService.AuthService.Implementation
{
    public class AuthService : IAuthService
    {
        private readonly IJSRuntime _jSRuntime;
        private readonly NavigationManager _navigationManager;
        private HubConnection? authHubConnection = null;
        private ConcurrentQueue<Func<bool, Task>> RefreshTokenCallbackQueue = new();
        private ConcurrentQueue<Func<bool, Task>> IsTokenValidCallbackQueue = new();
        public AuthService
        (IJSRuntime jSRuntime,
        NavigationManager navigationManager)
        {
            _jSRuntime = jSRuntime;
            _navigationManager = navigationManager;
        }
        public async Task<HubConnection> ConnectAsync()
        {
            if (authHubConnection != null)
            {
                if (authHubConnection.State != HubConnectionState.Connected)
                {
                    await authHubConnection.StopAsync();
                    await authHubConnection.StartAsync();
                }
                return authHubConnection;
            }
            authHubConnection = new HubConnectionBuilder()
            .WithUrl(_navigationManager.ToAbsoluteUri("/authHub"))
            .Build();

            authHubConnection.On<AuthResult>("OnTokensRefresh", async result =>
            {
                bool isRefreshSucceeded = false;
                if (result.Result == AuthResultType.Success)
                {
                    JWTPair? jwtPair = result.JWTPair;
                    await _jSRuntime.InvokeVoidAsync("localStorage.setItem", "access-token", jwtPair!.AccessToken);
                    await _jSRuntime.InvokeVoidAsync("localStorage.setItem", "refresh-token", jwtPair!.RefreshToken.Token);

                    isRefreshSucceeded = true;
                }
                CallbackExecutor.ExecuteCallbackQueue(isRefreshSucceeded, RefreshTokenCallbackQueue);
            });

            authHubConnection.On<bool>("OnTokenValidation", isTokenValid =>
            {
                CallbackExecutor.ExecuteCallbackQueue(isTokenValid, IsTokenValidCallbackQueue);
            });

            await authHubConnection.StartAsync();

            return authHubConnection;
        }

        public async Task DisconnectAsync()
        {
            if (authHubConnection != null)
            {
                if (authHubConnection.State != HubConnectionState.Disconnected)
                {
                    await authHubConnection.StopAsync();
                }
            }
        }

        public async Task RefreshTokenIfNeededAsync(Func<bool, Task> callback)
        {
            JWTPair? jwtPair = await GetJWTPairAsync();
            if (jwtPair == null)
            {
                await callback(false);
            }
            else
            {
                if (TokenReader.HasAccessTokenExpired(jwtPair.AccessToken))
                {
                    RefreshTokenCallbackQueue.Enqueue(callback);
                    await authHubConnection!.SendAsync("RefreshTokens", new RefreshToken
                    {
                        Token = (jwtPair.RefreshToken.Token)
                    });
                }
                else
                {
                    await callback(true);
                }
            }
        }

        public async Task ValidateTokenAsync(Func<bool, Task> callback)
        {
            JWTPair? jWTPair = await GetJWTPairAsync();
            if (jWTPair == null)
            {
                await callback(false);
            }
            else
            {
                IsTokenValidCallbackQueue.Enqueue(callback);
                await authHubConnection!.SendAsync("IsTokenValid", jWTPair.AccessToken);
            }
        }

        private async Task<JWTPair?> GetJWTPairAsync()
        {
            string? accessToken = await JWTHelper.GetAccessToken(_jSRuntime);
            string? refreshToken = await JWTHelper.GetRefreshToken(_jSRuntime);

            if (string.IsNullOrWhiteSpace(accessToken)
               ||
               string.IsNullOrWhiteSpace(refreshToken))
            {
                return null;
            }

            return new JWTPair
            {
                AccessToken = accessToken,
                RefreshToken = new RefreshToken
                {
                    Token = refreshToken
                }
            };
        }
        public async Task DisconnectedAsync()
        {
            await authHubConnection.StopAsync();
            await authHubConnection.DisposeAsync();
            authHubConnection = null;
        }
    }
}
