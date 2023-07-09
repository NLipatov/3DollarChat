using Limp.Client.HubInteraction.Handlers.Helpers;
using Limp.Client.Services.HubServices.CommonServices;
using Limp.Client.Services.HubServices.CommonServices.CallbackExecutor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using System.Collections.Concurrent;
using Limp.Client.Services.JWTReader;
using LimpShared.Models.Authentication.Models;
using LimpShared.Models.Authentication.Models.UserAuthentication;

namespace Limp.Client.Services.HubService.AuthService.Implementation
{
    public class AuthService : IAuthService
    {
        private readonly IJSRuntime _jSRuntime;
        private readonly NavigationManager _navigationManager;
        private readonly ICallbackExecutor _callbackExecutor;
        private HubConnection? hubConnection = null;
        private ConcurrentQueue<Func<bool, Task>> RefreshTokenCallbackQueue = new();
        private ConcurrentQueue<Func<bool, Task>> IsTokenValidCallbackQueue = new();
        public AuthService
        (IJSRuntime jSRuntime,
        NavigationManager navigationManager,
        ICallbackExecutor callbackExecutor)
        {
            _jSRuntime = jSRuntime;
            _navigationManager = navigationManager;
            _callbackExecutor = callbackExecutor;
        }
        public async Task<HubConnection> ConnectAsync()
        {
            HubConnection? existingHubConnection = await TryGetExistingHubConnection();
            if (existingHubConnection != null)
            {
                return existingHubConnection;
            }

            hubConnection = new HubConnectionBuilder()
            .WithUrl(_navigationManager.ToAbsoluteUri("/authHub"))
            .Build();

            hubConnection.On<AuthResult>("OnTokensRefresh", async result =>
            {
                bool isRefreshSucceeded = false;
                if (result.Result == AuthResultType.Success)
                {
                    JWTPair? jwtPair = result.JWTPair;
                    await _jSRuntime.InvokeVoidAsync("localStorage.setItem", "access-token", jwtPair!.AccessToken);
                    await _jSRuntime.InvokeVoidAsync("localStorage.setItem", "refresh-token", jwtPair!.RefreshToken.Token);

                    isRefreshSucceeded = true;
                }
                _callbackExecutor.ExecuteCallbackQueue(isRefreshSucceeded, RefreshTokenCallbackQueue);
            });

            hubConnection.On<bool>("OnTokenValidation", isTokenValid =>
            {
                _callbackExecutor.ExecuteCallbackQueue(isTokenValid, IsTokenValidCallbackQueue);
            });

            hubConnection.On<AuthResult>("OnLoggingIn", async result =>
            {
                _callbackExecutor.ExecuteSubscriptionsByName(result, "OnLogIn");
            });

            await hubConnection.StartAsync();

            return hubConnection;
        }

        private async Task<HubConnection?> TryGetExistingHubConnection()
        {
            if (hubConnection != null)
            {
                if (hubConnection.State != HubConnectionState.Connected)
                {
                    await hubConnection.StopAsync();
                    await hubConnection.StartAsync();
                }
                return hubConnection;
            }
            return null;
        }

        public async Task DisconnectAsync()
        {
            if (hubConnection != null)
            {
                if (hubConnection.State != HubConnectionState.Disconnected)
                {
                    await hubConnection.StopAsync();
                }
            }
        }

        public async Task RenewalAccessTokenIfExpiredAsync(Func<bool, Task> isRenewalSucceededCallback)
        {
            JWTPair? jwtPair = await GetJWTPairAsync();
            if (jwtPair == null)
            {
                await isRenewalSucceededCallback(false);
            }
            else
            {
                if (TokenReader.HasAccessTokenExpired(jwtPair.AccessToken))
                {
                    RefreshTokenCallbackQueue.Enqueue(isRenewalSucceededCallback);
                    await hubConnection!.SendAsync("RefreshTokens", new RefreshToken
                    {
                        Token = (jwtPair.RefreshToken.Token)
                    });
                }
                else
                {
                    await isRenewalSucceededCallback(true);
                }
            }
        }

        public async Task ValidateAccessTokenAsync(Func<bool, Task> isTokenAccessValidCallback)
        {
            JWTPair? jWTPair = await GetJWTPairAsync();
            if (jWTPair == null)
            {
                await isTokenAccessValidCallback(false);
            }
            else
            {
                //Server will trigger callback execution later
                //later comes, when server responds us by calling client 'OnTokenValidation' method with boolean value
                IsTokenValidCallbackQueue.Enqueue(isTokenAccessValidCallback);
                //Informing server that we're waiting for it's decision on access token
                await hubConnection!.SendAsync("IsTokenValid", jWTPair.AccessToken);
            }
        }

        private async Task<JWTPair?> GetJWTPairAsync()
        {
            string? accessToken = await JWTHelper.GetAccessTokenAsync(_jSRuntime);
            string? refreshToken = await JWTHelper.GetRefreshTokenAsync(_jSRuntime);

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
            await HubDisconnecter.DisconnectAsync(hubConnection);
            hubConnection = null;
        }

        public bool IsConnected()
        {
            if(hubConnection == null)
                return false;

            return hubConnection.State == HubConnectionState.Connected;
        }

        public async Task LogIn(UserAuthentication userAuthentication)
        {
            if (hubConnection == null)
                throw new ApplicationException("No connection with Hub.");

            await hubConnection.SendAsync("LogIn", userAuthentication);
        }
    }
}
