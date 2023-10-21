using System.Collections.Concurrent;
using Limp.Client.HubInteraction.Handlers.Helpers;
using Limp.Client.Services.HubServices.CommonServices.CallbackExecutor;
using Limp.Client.Services.HubServices.CommonServices.HubServiceConnectionBuilder;
using Limp.Client.Services.JWTReader;
using Limp.Client.Services.UserAgentService;
using LimpShared.Models.Authentication.Models;
using LimpShared.Models.Authentication.Models.UserAuthentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace Limp.Client.Services.HubServices.HubServices.Implementations.AuthService.Implementation
{
    public class AuthService : IAuthService
    {
        public NavigationManager NavigationManager { get; set; }
        private readonly IJSRuntime _jSRuntime;
        private readonly ICallbackExecutor _callbackExecutor;
        private readonly IUserAgentService _userAgentService;
        private HubConnection? HubConnectionInstance { get; set; }
        private ConcurrentQueue<Func<bool, Task>> RefreshTokenCallbackQueue = new();
        private ConcurrentQueue<Func<bool, Task>> IsTokenValidCallbackQueue = new();
        public AuthService
        (IJSRuntime jSRuntime,
        NavigationManager navigationManager,
        ICallbackExecutor callbackExecutor,
        IUserAgentService userAgentService)
        {
            _jSRuntime = jSRuntime;
            NavigationManager = navigationManager;
            _callbackExecutor = callbackExecutor;
            _userAgentService = userAgentService;
            InitializeHubConnection();
            RegisterHubEventHandlers();
        }

        private void InitializeHubConnection()
        {
            if (HubConnectionInstance is not null)
                return;
            
            HubConnectionInstance = HubServiceConnectionBuilder
                .Build(NavigationManager.ToAbsoluteUri("/authHub"));
        }

        public async Task<HubConnection> GetHubConnectionAsync()
        {
            if (HubConnectionInstance == null)
                throw new ArgumentException($"{nameof(HubConnectionInstance)} was not properly instantiated.");
            
            while (HubConnectionInstance.State is not  HubConnectionState.Connected)
            {
                try
                {
                    if (HubConnectionInstance.State is not HubConnectionState.Disconnected)
                        await HubConnectionInstance.StopAsync();

                    await HubConnectionInstance.StartAsync();
                }
                catch
                {
                    return await GetHubConnectionAsync();
                }
            }

            return HubConnectionInstance;
        }

        private void RegisterHubEventHandlers()
        {
            if (HubConnectionInstance is null)
                throw new NullReferenceException($"Could not register event handlers - hub was null.");

            HubConnectionInstance.On<AuthResult>("OnTokensRefresh", async result =>
            {
                bool isRefreshSucceeded = false;
                if (result.Result == AuthResultType.Success)
                {
                    JwtPair? jwtPair = result.JwtPair;
                    await _jSRuntime.InvokeVoidAsync("localStorage.setItem", "access-token", jwtPair!.AccessToken);
                    await _jSRuntime.InvokeVoidAsync("localStorage.setItem", "refresh-token", jwtPair!.RefreshToken.Token);

                    isRefreshSucceeded = true;
                }
                _callbackExecutor.ExecuteCallbackQueue(isRefreshSucceeded, RefreshTokenCallbackQueue);
            });

            HubConnectionInstance.On<bool>("OnTokenValidation", isTokenValid =>
            {
                _callbackExecutor.ExecuteCallbackQueue(isTokenValid, IsTokenValidCallbackQueue);
            });

            HubConnectionInstance.On<AuthResult>("OnLoggingIn", result =>
            {
                _callbackExecutor.ExecuteSubscriptionsByName(result, "OnLogIn");
            });

            HubConnectionInstance.On<List<AccessRefreshEventLog>>("OnRefreshTokenHistoryResponse", async result =>
            {
                _callbackExecutor.ExecuteSubscriptionsByName(result, "OnRefreshTokenHistoryResponse");
            });
        }

        public async Task RenewalAccessTokenIfExpiredAsync(Func<bool, Task> isRenewalSucceededCallback)
        {
            var hubConnection = await GetHubConnectionAsync();
            JwtPair? jwtPair = await GetJWTPairAsync();
            if (jwtPair == null)
            {
                await isRenewalSucceededCallback(false);
            }
            else
            {
                if (TokenReader.HasAccessTokenExpired(jwtPair.AccessToken))
                {
                    var userAgentInformation = await _userAgentService.GetUserAgentInformation();
                    
                    RefreshTokenCallbackQueue.Enqueue(isRenewalSucceededCallback);
                    
                    await hubConnection.SendAsync("RefreshTokens", new RefreshTokenDto
                    {
                        RefreshToken = jwtPair.RefreshToken,
                        UserAgent = userAgentInformation.UserAgentDescription,
                        UserAgentId = userAgentInformation.UserAgentId
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
            var hubConnection = await GetHubConnectionAsync();
            JwtPair? jWTPair = await GetJWTPairAsync();
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
                await hubConnection.SendAsync("IsTokenValid", jWTPair.AccessToken);
            }
        }

        private async Task<JwtPair?> GetJWTPairAsync()
        {
            string? accessToken = await JWTHelper.GetAccessTokenAsync(_jSRuntime);
            string? refreshToken = await JWTHelper.GetRefreshTokenAsync(_jSRuntime);

            if (string.IsNullOrWhiteSpace(accessToken)
               ||
               string.IsNullOrWhiteSpace(refreshToken))
            {
                return null;
            }

            return new JwtPair
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
            if (HubConnectionInstance is not null)
                await HubConnectionInstance.StopAsync();
        }

        public async Task LogIn(UserAuthentication userAuthentication)
        {
            var hubConnection = await GetHubConnectionAsync();

            await hubConnection.SendAsync("LogIn", userAuthentication);
        }

        public async Task GetRefreshTokenHistory()
        {
            var hubConnection = await GetHubConnectionAsync();
            
            var accessToken = await JWTHelper.GetAccessTokenAsync(_jSRuntime);

            await hubConnection.SendAsync("GetTokenRefreshHistory", accessToken);
        }
    }
}
