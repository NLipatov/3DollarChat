using Ethachat.Client.Services.HubServices.CommonServices.SubscriptionService;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.AuthService;
using Ethachat.Client.Services.UserAgentService;
using LimpShared.Models.Authentication.Models;
using LimpShared.Models.Authentication.Models.UserAuthentication;
using Microsoft.JSInterop;

namespace Ethachat.Client.Pages.AccountManagement.LogicHandlers
{
    public class LoginHandler : IDisposable, ILoginHandler
    {
        private readonly IJSRuntime _jSRuntime;
        private readonly IAuthService _authService;
        private readonly IHubServiceSubscriptionManager _hubServiceSubscriptionManager;
        private readonly IUserAgentService _userAgentService;

        private Action<AuthResult> _onLogInResponseCallback { get; set; }

        private Guid ComponentId { get; set; }
        public void Dispose()
        {
            _hubServiceSubscriptionManager.RemoveComponentCallbacks(ComponentId);
        }

        public LoginHandler
            (IJSRuntime jSRuntime,
            IAuthService authService,
            IHubServiceSubscriptionManager hubServiceSubscriptionManager,
            IUserAgentService userAgentService)
        {
            _jSRuntime = jSRuntime;
            _authService = authService;
            _hubServiceSubscriptionManager = hubServiceSubscriptionManager;
            _userAgentService = userAgentService;

            //This id will be needed on dispose stage
            //On dispose stage we need to clear out all of the component event subscriptions
            ComponentId = Guid.NewGuid();

            //Subscribing to server event of updating online users
            _hubServiceSubscriptionManager
                .AddCallback<AuthResult>(HandleOnLogInResponse, "OnLogIn", ComponentId);
        }

        private async Task HandleOnLogInResponse(AuthResult authResult)
        {
            if (authResult.Result != AuthResultType.Fail)
            {
                await StoreTokensAsync(authResult);
            }

            _onLogInResponseCallback.Invoke(authResult);
        }
        public async Task OnLogIn(UserAuthentication loginEventInformation, Action<AuthResult> callback)
        {
            var userAgentInformation = await _userAgentService.GetUserAgentInformation();

            loginEventInformation.UserAgent = userAgentInformation.UserAgentDescription ?? string.Empty;
            loginEventInformation.UserAgentId = userAgentInformation.UserAgentId;
            
            _onLogInResponseCallback = callback;

            await _authService.LogIn(loginEventInformation);
        }
        private async Task StoreTokensAsync(AuthResult result)
        {
            if (result is null)
                throw new ArgumentException($"{nameof(AuthResult)} parameter was null.");

            if (string.IsNullOrWhiteSpace(result?.JwtPair?.AccessToken) || string.IsNullOrWhiteSpace(result?.JwtPair?.RefreshToken?.Token))
            {
                throw new ArgumentException("Server authentification response was invalid");
            }

            await _jSRuntime.InvokeVoidAsync("localStorage.setItem", "access-token", result!.JwtPair!.AccessToken);
            await _jSRuntime.InvokeVoidAsync("localStorage.setItem", "refresh-token", result.JwtPair.RefreshToken.Token);
        }
    }
}