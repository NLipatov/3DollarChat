using ClientServerCommon.Models;
using Limp.Client.Cryptography;
using Limp.Client.HubConnectionManagement.HubObservers.Implementations.MessageHub.EventTypes;
using Limp.Client.HubInteraction.Handlers.Helpers;
using Limp.Client.HubInteraction.Handlers.MessageDispatcherHub.AESOfferHandling;
using Limp.Client.HubInteraction.HubObservers;
using Limp.Client.HubInteraction.HubObservers.Implementations.UsersHubObserver.EventTypes;
using Limp.Client.Services.HubConnectionProvider.Implementation.HubInteraction.Implementations;
using Limp.Client.Services.HubService.AuthService;
using Limp.Client.Services.HubService.UsersService;
using Limp.Client.TopicStorage;
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
        IHubObserver<MessageHubEvent> messageDispatcherHubObserver,
        ICryptographyService cryptographyService,
        IAESOfferHandler aESOfferHandler,
        IMessageBox messageBox,
        IAuthService authService,
        IUsersService usersService)
        {
            _jSRuntime = jSRuntime;
            _navigationManager = navigationManager;
            _messageDispatcherHubObserver = messageDispatcherHubObserver;
            _cryptographyService = cryptographyService;
            _aESOfferHandler = aESOfferHandler;
            _messageBox = messageBox;
            _authService = authService;
            _usersService = usersService;
        }
        private MessageDispatcherHubInteractor? _messageDispatcherHubInteractor;
        private readonly IJSRuntime _jSRuntime;
        private readonly NavigationManager _navigationManager;
        private readonly IHubObserver<MessageHubEvent> _messageDispatcherHubObserver;
        private readonly ICryptographyService _cryptographyService;
        private readonly IAESOfferHandler _aESOfferHandler;
        private readonly IMessageBox _messageBox;
        private readonly IAuthService _authService;
        private readonly IUsersService _usersService;
        private List<Guid> messageDispatcherHandlers = new();
        private HubConnection authHubConnection;
        private HubConnection? usersHubConnection;

        public async Task ConnectToHubs()
        {
            //If user does not have at least one token from JWT pair, ask him to login
            string? accessToken = await JWTHelper.GetAccessToken(_jSRuntime);
            if (string.IsNullOrWhiteSpace(accessToken)
                ||
                string.IsNullOrWhiteSpace(await JWTHelper.GetRefreshToken(_jSRuntime)))
            {
                _navigationManager.NavigateTo("login");
                return;
            }

            authHubConnection = await _authService.ConnectAsync();
            await RefreshTokenIfNeededAsync();
        }

        private async Task RefreshTokenIfNeededAsync()
        {
            await _authService.RefreshTokenIfNeededAsync(async (isRefreshSucceeded) =>
            {
                if (isRefreshSucceeded)
                {
                    await DecideOnToken();
                }
                else
                {
                    await DenyHandle();
                }
            });
        }

        private async Task DecideOnToken()
        {
            await _authService.ValidateTokenAsync(async (isTokenValid) =>
            {
                if (isTokenValid)
                {
                    await ProceedHandle();
                }
                else
                {
                    await DenyHandle();
                }
            });
        }

        private async Task DenyHandle()
        {
            _navigationManager.NavigateTo("/login");
        }
        private async Task ProceedHandle()
        {
            await _usersService.ConnectAsync();

            //Creating interactor, but not connecting to hub
            //We will connect to this hub only when client's username is resolved
            _messageDispatcherHubInteractor = new MessageDispatcherHubInteractor
                (_navigationManager,
                _jSRuntime,
                _messageDispatcherHubObserver,
                _cryptographyService,
                _aESOfferHandler,
                _messageBox,
                usersHubConnection,
                authHubConnection);
        }

        private async Task InvokeCallbackIfExists<T>(Func<T, Task>? callback, T parameter)
        {
            if (callback != null)
                await callback.Invoke(parameter);
        }

        public async ValueTask DisposeAsync()
        {
            await _usersService.DisconnectAsync();
            await _authService.DisconnectAsync();

            if (_messageDispatcherHubInteractor != null)
            {
                await _messageDispatcherHubInteractor.DisposeAsync();
            }
        }
    }
}
