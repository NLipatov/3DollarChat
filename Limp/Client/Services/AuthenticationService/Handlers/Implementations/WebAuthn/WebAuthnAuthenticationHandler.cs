using Limp.Client.Services.HubServices.HubServices.Implementations.AuthService;
using Limp.Client.Services.LocalStorageService;
using LimpShared.Models.Authentication.Models;
using LimpShared.Models.Authentication.Models.Credentials;
using LimpShared.Models.Authentication.Models.Credentials.CredentialsDTO;
using LimpShared.Models.Authentication.Models.Credentials.Implementation;
using LimpShared.Models.Authentication.Types;
using Microsoft.AspNetCore.SignalR.Client;

namespace Limp.Client.Services.AuthenticationService.Handlers.Implementations.WebAuthn;

public class WebAuthnAuthenticationHandler : IWebAuthnHandler
{
    private readonly ILocalStorageService _localStorageService;

    public async Task<ICredentials> GetCredentials()
    {
        return new WebAuthnPair()
        {
            Counter = uint.Parse(await GetRefreshCredential()),
            CredentialId = await GetAccessCredential()
        };
    }

    private async Task<string> GetCredentialId() =>
        await _localStorageService.ReadPropertyAsync("credentialId") ?? string.Empty;

    private async Task<string> GetCounter() =>
        await _localStorageService.ReadPropertyAsync("credentialIdCounter") ?? string.Empty;

    public async Task<string> GetRefreshCredential() => await GetCounter();

    public async Task<string> GetAccessCredential() => await GetCredentialId();

    public WebAuthnAuthenticationHandler(ILocalStorageService localStorageService)
    {
        _localStorageService = localStorageService;
    }

    public async Task<AuthenticationType?> GetAuthenticationTypeAsync()
    {
        return AuthenticationType.WebAuthn;
    }

    public async Task<string> GetUsernameAsync() =>
        await _localStorageService.ReadPropertyAsync("credentialUsername") ?? string.Empty;

    public async Task<bool> IsSetToUseAsync()
    {
        WebAuthnPair webAuthnPair = await GetWebAuthnPairAsync();
        return !string.IsNullOrWhiteSpace(webAuthnPair.CredentialId);
    }

    public async Task TriggerCredentialsValidation(HubConnection hubConnection)
    {
        var pair = await GetWebAuthnPairAsync();

        await hubConnection.SendAsync("ValidateCredentials", new CredentialsDTO {WebAuthnPair = new WebAuthnPair()
            {
                CredentialId = pair.CredentialId,
                Counter = pair.Counter
            }});
    }

    public async Task UpdateCredentials(ICredentials newCredentials)
    {
        var counter = await GetCounter();
        var updatedCounter = counter + 1;
        await _localStorageService.WritePropertyAsync("credentialIdCounter", updatedCounter);
    }

    private async Task<WebAuthnPair> GetWebAuthnPairAsync()
    {
        var credentialId = await GetCredentialId();
        var counter = await GetCounter();

        if (string.IsNullOrWhiteSpace(credentialId))
        {
            return new WebAuthnPair
            {
                Counter = 0,
                CredentialId = string.Empty
            };
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
}