using Ethachat.Client.Services.Authentication.Handlers;
using Ethachat.Client.Services.Authentication.Stages;
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

        subscriptionManager
            .AddCallback<bool>(async isVisible 
                =>
            {
                if (!isVisible)
                {
                    authService.PreventReconnecting();
                    usersService.PreventReconnecting();
                    messageService.PreventReconnecting();
                }
                else
                {
                    await Task.WhenAll(
                        authService.ReconnectAsync(),
                        usersService.ReconnectAsync(),
                        messageService.ReconnectAsync());
                }
            }, "AppVisibilityStateChanged", ComponentId);

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
                AuthenticationState = AuthenticationState.Authenticated;

                await Task.WhenAll(
                    authenticationHandler.ExecutePostCredentialsValidation(credentialsValidationResult, await authService.GetHubConnectionAsync()), 
                    usersService.GetHubConnectionAsync(), 
                    messageService.GetHubConnectionAsync());
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