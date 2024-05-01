using System.Collections.Concurrent;
using Ethachat.Client.Services.AuthenticationService.Handlers;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;
using Ethachat.Client.Services.HubServices.HubServices.Builders;
using Ethachat.Client.Services.LocalStorageService;
using Ethachat.Client.Services.UserAgent;
using EthachatShared.Constants;
using EthachatShared.Models.Authentication.Models;
using EthachatShared.Models.Authentication.Models.Credentials.Implementation;
using EthachatShared.Models.Authentication.Models.UserAuthentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.AuthService.Implementation
{
    public class AuthService : IAuthService
    {
        public NavigationManager NavigationManager { get; set; }
        private readonly ICallbackExecutor _callbackExecutor;
        private readonly IUserAgentService _userAgentService;
        private readonly ILocalStorageService _localStorageService;
        private bool _isConnectionClosedCallbackSet = false;
        private HubConnection? HubConnectionInstance { get; set; }
        private ConcurrentQueue<Func<bool, Task>> RefreshTokenCallbackQueue = new();
        public ConcurrentQueue<Func<AuthResult, Task>> IsTokenValidCallbackQueue { get; set; } = new();
        private readonly IAuthenticationHandler _authenticationManager;
        private readonly IConfiguration _configuration;

        public AuthService
        (NavigationManager navigationManager,
        ICallbackExecutor callbackExecutor,
        IUserAgentService userAgentService,
        ILocalStorageService localStorageService,
        IAuthenticationHandler authenticationManager,
        IConfiguration configuration)
        {
            NavigationManager = navigationManager;
            _callbackExecutor = callbackExecutor;
            _userAgentService = userAgentService;
            _localStorageService = localStorageService;
            _authenticationManager = authenticationManager;
            _configuration = configuration;
            InitializeHubConnection();
            RegisterHubEventHandlers();
        }

        private void InitializeHubConnection()
        {
            if (HubConnectionInstance is not null)
                return;
            
            HubConnectionInstance = HubServiceConnectionBuilder
                .Build(NavigationManager.ToAbsoluteUri(HubRelativeAddresses.AuthHubRelativeAddress));
        }

        public async Task<HubConnection> GetHubConnectionAsync()
        {
            if (HubConnectionInstance == null)
                throw new ArgumentException($"{nameof(HubConnectionInstance)} was not properly instantiated.");
            
            while (HubConnectionInstance.State is HubConnectionState.Disconnected)
            {
                try
                {
                    await HubConnectionInstance.StartAsync();
                }
                catch
                {
                    var interval = int.Parse(_configuration["HubConnection:ReconnectionIntervalMs"] ?? "0");
                    await Task.Delay(interval);
                    await GetHubConnectionAsync();
                    break;
                }
            }
            
            _callbackExecutor.ExecuteSubscriptionsByName(true, "OnAuthHubConnectionStatusChanged");

            if (_isConnectionClosedCallbackSet is false)
            {
                HubConnectionInstance.Closed += OnConnectionLost;
                _isConnectionClosedCallbackSet = true;
            }

            return HubConnectionInstance;
        }

        private async Task OnConnectionLost(Exception? arg)
        {
            _callbackExecutor.ExecuteSubscriptionsByName(false, "OnAuthHubConnectionStatusChanged");
            await GetHubConnectionAsync();
        }

        private void RegisterHubEventHandlers()
        {
            if (HubConnectionInstance is null)
                throw new NullReferenceException($"Could not register event handlers - hub was null.");

            HubConnectionInstance.On<AuthResult>("OnRefreshCredentials", async result =>
            {
                if (result.Result is not AuthResultType.Success)
                    NavigationManager.NavigateTo("signin");
                
                if (result.JwtPair is not null)
                    await _authenticationManager.UpdateCredentials(result.JwtPair);

                _callbackExecutor.ExecuteSubscriptionsByName(result, "OnRefreshCredentials");
            });

            HubConnectionInstance.On<AuthResult>("OnValidateCredentials", async result =>
            {
                _callbackExecutor.ExecuteSubscriptionsByName(result, "OnValidateCredentials");
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

            HubConnectionInstance.On<AuthResult>("OnRegister", result =>
            {
                _callbackExecutor.ExecuteSubscriptionsByName(result, "OnRegister");
            });
        }

        public async Task ValidateAccessTokenAsync(Func<AuthResult, Task> isTokenAccessValidCallback)
        {
            var authenticationIsReadyToUse = await _authenticationManager.IsSetToUseAsync();
            if (!authenticationIsReadyToUse)
            {
                await isTokenAccessValidCallback(new AuthResult(){Result = AuthResultType.Fail});
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

        public async Task Register(UserAuthentication newUserDto)
        {
            var hubConnection = await GetHubConnectionAsync();
            
            await hubConnection.SendAsync("Register", newUserDto);
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
