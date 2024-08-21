using Client.Application.Gateway;
using Ethachat.Client.Services.LocalStorageService;
using EthachatShared.Models.Authentication.Models;
using EthachatShared.Models.Authentication.Models.Credentials;
using EthachatShared.Models.Authentication.Models.Credentials.CredentialsDTO;
using EthachatShared.Models.Authentication.Models.Credentials.Implementation;
using EthachatShared.Models.Authentication.Types;

namespace Ethachat.Client.Services.AuthenticationService.Handlers.Implementations.WebAuthn;

public class WebAuthnAuthenticationHandler(ILocalStorageService localStorageService) : IWebAuthnHandler
{
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
        await localStorageService.ReadPropertyAsync("credentialId") ?? string.Empty;

    private async Task<string> GetCounter() =>
        await localStorageService.ReadPropertyAsync("credentialIdCounter") ?? string.Empty;

    public async Task<string> GetRefreshCredential() => await GetCounter();

    public async Task<string> GetAccessCredential() => await GetCredentialId();

    public Task<AuthenticationType?> GetAuthenticationTypeAsync()
        => Task.FromResult(AuthenticationType.WebAuthn as AuthenticationType?);

    public async Task<string> GetUsernameAsync() =>
        await localStorageService.ReadPropertyAsync("credentialUsername") ?? string.Empty;

    public async Task<bool> IsSetToUseAsync()
    {
        WebAuthnPair webAuthnPair = await GetWebAuthnPairAsync();
        return !string.IsNullOrWhiteSpace(webAuthnPair.CredentialId);
    }

    public async Task TriggerCredentialsValidation(IGateway gateway)
    {
        var dto = await GetCredentialsDto();

        if (dto.WebAuthnPair is null)
            dto.WebAuthnPair = new();

        await gateway.SendAsync("ValidateCredentials", dto);
    }

    public async Task UpdateCredentials(ICredentials newCredentials)
    {
        var dto = await GetCredentialsDto();

        if (dto.WebAuthnPair is null)
            throw new ArgumentException("Invalid credentials stored in local storage.");

        uint updatedCounter = dto.WebAuthnPair.Counter + 1;
        await localStorageService.WritePropertyAsync("credentialId", dto.WebAuthnPair.CredentialId);
        await localStorageService.WritePropertyAsync("credentialIdCounter", updatedCounter.ToString());
        await localStorageService.WritePropertyAsync("CredentialUpdatedOn", DateTime.UtcNow.ToString("s"));
    }

    public async Task ExecutePostCredentialsValidation(AuthResult result, IGateway gateway)
    {
        if ((DateTime.UtcNow - await GetCredentialsUpdatedOn()).TotalMinutes < 5)
            return;

        var dto = await GetCredentialsDto();

        if (result.Result == AuthResultType.Success)
            await gateway.SendAsync("RefreshCredentials", dto);

        await UpdateCredentials(new WebAuthnPair
        {
            Counter = dto.WebAuthnPair!.Counter,
            CredentialId = dto.WebAuthnPair.CredentialId
        });
    }

    private async Task<DateTime> GetCredentialsUpdatedOn()
    {
        var dateString = await localStorageService.ReadPropertyAsync("CredentialUpdatedOn");

        if (DateTime.TryParse(dateString, out var updatedOn))
            return updatedOn;

        return DateTime.MinValue;
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
            await localStorageService.WritePropertyAsync("credentialIdCounter", "0");
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