using System.Collections.Concurrent;
using Ethachat.Client.Services.LocalStorageService;
using EthachatShared.Models.Authentication.Models;
using EthachatShared.Models.Authentication.Models.Credentials;
using EthachatShared.Models.Authentication.Models.Credentials.CredentialsDTO;
using EthachatShared.Models.Authentication.Models.Credentials.Implementation;
using EthachatShared.Models.Authentication.Types;
using Microsoft.AspNetCore.SignalR.Client;

namespace Ethachat.Client.Services.AuthenticationService.Handlers.Implementations.WebAuthn;

public class WebAuthnAuthenticationHandler : IWebAuthnHandler
{
    private readonly ILocalStorageService _localStorageService;

    public async Task<CredentialsDTO> GetCredentialsDto()
    {
        return new CredentialsDTO()
        {
            WebAuthnPair = (await GetCredentials()) as WebAuthnPair
        };
    }

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
        if(credentialsValidationCounter.Count != 0)
            return;
        
        credentialsValidationCounter.Push(true);
        
        var dto = await GetCredentialsDto();
        
        if (dto.WebAuthnPair is null)
            dto.WebAuthnPair = new();
        
        await hubConnection.SendAsync("ValidateCredentials",  dto);
    }

    public async Task UpdateCredentials(ICredentials newCredentials)
    {
        var dto = await GetCredentialsDto();

        if (dto.WebAuthnPair is null)
            throw new ArgumentException(
                $"Exception:{nameof(WebAuthnAuthenticationHandler)}.{nameof(UpdateCredentials)}:" +
                $"Invalid credentials stored in local storage.");

        uint updatedCounter = dto.WebAuthnPair.Counter + 1;
        await _localStorageService.WritePropertyAsync("credentialId", dto.WebAuthnPair.CredentialId);
        await _localStorageService.WritePropertyAsync("credentialIdCounter", updatedCounter.ToString());
        
        credentialsValidationCounter.TryPop(out _);
    }

    //Each item is an ongoing process of WebAuthn credentials validation.
    //If there'll be more than one process, it can lead to the discrepancy of the counter on the backend and frontend
    private static ConcurrentStack<bool> credentialsValidationCounter = new();
    public async Task ExecutePostCredentialsValidation(AuthResult result, HubConnection hubConnection)
    {
        if (credentialsValidationCounter.Count != 1)
        {
            credentialsValidationCounter.TryPop(out _);
            return;
        }
        
        var dto = await GetCredentialsDto();
        
        if (result.Result == AuthResultType.Success)
            await hubConnection.SendAsync("RefreshCredentials", dto);
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