using Ethachat.Client.Services.Authentication.Boundaries.Stages;
using Ethachat.Client.Services.Authentication.Handlers;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;
using Ethachat.Client.Services.HubServices.CommonServices.SubscriptionService;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.AuthService;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.UsersService;
using EthachatShared.Models.Authentication.Models;
using Microsoft.AspNetCore.Components;

namespace Ethachat.Client.Services.Authentication.Boundaries;

public class AuthenticationManagerBoundary(
    IAuthenticationHandler authenticationHandler,
    IHubServiceSubscriptionManager subscriptionManager,
    IAuthService authService,
    IMessageService messageService,
    IUsersService usersService,
    NavigationManager navigationManager,
    ICallbackExecutor callbackExecutor) : IAuthenticationManagerBoundary
{
    public AuthenticationState AuthenticationState { get; private set; } = AuthenticationState.TokenActualisation;
    private AuthenticationState LastAuthenticationState { get; set; } = AuthenticationState.TokenActualisation;
    private Guid ComponentId { get; set; }

    public void Dispose() => subscriptionManager.RemoveComponentCallbacks(ComponentId);

    public async Task InitializeAsync()
    {
        if (!await authenticationHandler.IsSetToUseAsync())
        {
            navigationManager.NavigateTo("signin");
            return;
        }

        if (Guid.Empty == ComponentId)
            ComponentId = Guid.NewGuid();

        subscriptionManager
            .AddCallback<AuthResult>(HandleAuthenticationCheckResult, "OnRefreshCredentials", ComponentId);

        subscriptionManager
            .AddCallback<AuthResult>(HandleAuthenticationCheckResult, "OnValidateCredentials", ComponentId);

        await ValidateCredentials();
    }

    private async Task ValidateCredentials()
    {
        await authenticationHandler.TriggerCredentialsValidation(await authService.GetHubConnectionAsync());
    }

    private async Task HandleAuthenticationCheckResult(AuthResult credentialsValidationResult)
    {
        var isAccessTokenValid = credentialsValidationResult.Result is AuthResultType.Success;
        if (isAccessTokenValid)
        {
            if (AuthenticationState is not AuthenticationState.Authenticated)
            {
                await messageService.GetHubConnectionAsync();
                await usersService.GetHubConnectionAsync();
                await authService.GetHubConnectionAsync();

                AuthenticationState = AuthenticationState.Authenticated;

                await authenticationHandler
                    .ExecutePostCredentialsValidation(credentialsValidationResult,
                        await authService.GetHubConnectionAsync());
            }
        }
        else
        {
            AuthenticationState = AuthenticationState.NotAuthenticated;
        }

        if (LastAuthenticationState != AuthenticationState)
            callbackExecutor.ExecuteSubscriptionsByName("AuthenticationStateHasChanged");
            
        LastAuthenticationState = AuthenticationState;
    }
}