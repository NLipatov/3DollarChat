using System.Collections.Concurrent;
using Limp.Client.Services.AuthenticationService.Handlers;
using Limp.Client.Services.HubServices.CommonServices.CallbackExecutor;
using Limp.Client.Services.HubServices.CommonServices.HubServiceConnectionBuilder;
using Limp.Client.Services.JWTReader;
using Limp.Client.Services.LocalStorageService;
using Limp.Client.Services.UserAgentService;
using LimpShared.Models.Authentication.Models;
using LimpShared.Models.Authentication.Models.Credentials.Implementation;
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
        private readonly ILocalStorageService _localStorageService;
        private HubConnection? HubConnectionInstance { get; set; }
        private ConcurrentQueue<Func<bool, Task>> RefreshTokenCallbackQueue = new();
        public ConcurrentQueue<Func<bool, Task>> IsTokenValidCallbackQueue { get; set; } = new();
        private readonly IAuthenticationHandler _authenticationManager;

        public AuthService
        (IJSRuntime jSRuntime,
        NavigationManager navigationManager,
        ICallbackExecutor callbackExecutor,
        IUserAgentService userAgentService,
        ILocalStorageService localStorageService,
        IAuthenticationHandler authenticationManager)
        {
            _jSRuntime = jSRuntime;
            NavigationManager = navigationManager;
            _callbackExecutor = callbackExecutor;
            _userAgentService = userAgentService;
            _localStorageService = localStorageService;
            _authenticationManager = authenticationManager;
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
                if (result.Result == AuthResultType.Success)
                {
                    JwtPair? jwtPair = result.JwtPair;
                    await _jSRuntime.InvokeVoidAsync("localStorage.setItem", "access-token", jwtPair!.AccessToken);
                    await _jSRuntime.InvokeVoidAsync("localStorage.setItem", "refresh-token", jwtPair!.RefreshToken.Token);
                }
                _callbackExecutor.ExecuteCallbackQueue(result.Result == AuthResultType.Success, RefreshTokenCallbackQueue);
            });

            HubConnectionInstance.On<bool>("OnAuthenticationCredentialsValidated", async (isTokenValid) =>
            {
                WebAuthnPair? webAuthnPair = await GetWebAuthnPairAsync();
                
                _callbackExecutor.ExecuteSubscriptionsByName(isTokenValid, "OnAuthenticationCredentialsValidated");

                if (webAuthnPair is not null)
                {
                    var currentCounter = uint.Parse(await _localStorageService.ReadPropertyAsync("credentialIdCounter"));
                    await _localStorageService.WritePropertyAsync("credentialIdCounter", (currentCounter + 1).ToString());
                }
            });

            HubConnectionInstance.On<AuthResult>("OnLoggingIn", result =>
            {
                _callbackExecutor.ExecuteSubscriptionsByName(result, "OnLogIn");
            });

            HubConnectionInstance.On<List<AccessRefreshEventLog>>("OnRefreshTokenHistoryResponse", async result =>
            {
                _callbackExecutor.ExecuteSubscriptionsByName(result, "OnRefreshTokenHistoryResponse");
            });

            HubConnectionInstance.On<string>("AuthorisationServerAddressResolved", result =>
            {
                _callbackExecutor.ExecuteSubscriptionsByName(result, "OnAuthorisationServerAddressResponse");
            });

            HubConnectionInstance.On<AuthResult, Guid>("OnCredentialIdRefresh", async (result, eventId) =>
            {
                var currentCounter = uint.Parse(await _localStorageService.ReadPropertyAsync("credentialIdCounter") ?? "0");
                if (result.Result == AuthResultType.Success)
                {
                    await _localStorageService.WritePropertyAsync("credentialIdCounter", (currentCounter + 1).ToString());
                }
                
                _callbackExecutor.ExecuteCallbackQueue(result.Result == AuthResultType.Success, RefreshTokenCallbackQueue);
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

        public async Task RenewalCredentialId(Func<bool, Task> isRenewalSucceededCallback)
        {
            var hubConnection = await GetHubConnectionAsync();
            var credentialId = await _localStorageService.ReadPropertyAsync("credentialId");
            var credentialIdCounter = await _localStorageService.ReadPropertyAsync("credentialIdCounter");
            if (string.IsNullOrWhiteSpace(credentialId) || !uint.TryParse(credentialIdCounter, out var counter))
            {
                await isRenewalSucceededCallback(false);
            }
            else
            {
                var userAgentInformation = await _userAgentService.GetUserAgentInformation();
                
                RefreshTokenCallbackQueue.Enqueue(isRenewalSucceededCallback);
                
                await hubConnection.SendAsync("RefreshCredentialId", credentialId, counter+1);
            }
        }

        public async Task ValidateAccessTokenAsync(Func<bool, Task> isTokenAccessValidCallback)
        {
            var authenticationIsReadyToUse = await _authenticationManager.IsSetToUseAsync();
            if (!authenticationIsReadyToUse)
            {
                await isTokenAccessValidCallback(false);
            }
            else
            {
                //Server will trigger callback execution when server responds us by calling
                //client 'OnAuthenticationCredentialsValidated' method with boolean value
                IsTokenValidCallbackQueue.Enqueue(isTokenAccessValidCallback);
                
                //Informing server that we're waiting for it's decision on access token
                await _authenticationManager.TriggerCredentialsValidation(await GetHubConnectionAsync());
            }
        }

        private async Task<WebAuthnPair?> GetWebAuthnPairAsync()
        {
            var credentialId = await _localStorageService.ReadPropertyAsync("credentialId");
            var counter = await _localStorageService.ReadPropertyAsync("credentialIdCounter");

            if (string.IsNullOrWhiteSpace(credentialId))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(counter) || !uint.TryParse(counter, out var number))
            {
                await _localStorageService.WritePropertyAsync("credentialIdCounter", "0");
                return new WebAuthnPair()
                {
                    Counter = 0,
                    CredentialId = credentialId
                };
            }
            
            return new WebAuthnPair
            {
                CredentialId = credentialId,
                Counter = number
            };
        }

        private async Task<JwtPair?> GetJWTPairAsync()
        {
            string? accessToken = await _authenticationManager.GetAccessCredential();
            string? refreshToken = await _authenticationManager.GetRefreshCredential();

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
            
            var accessToken = await _authenticationManager.GetAccessCredential();

            await hubConnection.SendAsync("GetTokenRefreshHistory", accessToken);
        }

        public async Task GetAuthorisationServerAddress()
        {
            var hubConnection = await GetHubConnectionAsync();

            await hubConnection.SendAsync("GetAuthorisationServerAddress");
        }
    }
}
