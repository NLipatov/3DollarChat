using Ethachat.Client.Services.AuthenticationService.Handlers;
using Ethachat.Client.Services.AuthenticationService.Handlers.Implementations.Jwt;
using Ethachat.Client.Services.AuthenticationService.Handlers.Implementations.WebAuthn;
using LimpShared.Models.Authentication.Models;
using LimpShared.Models.Authentication.Models.Credentials;
using LimpShared.Models.Authentication.Models.Credentials.CredentialsDTO;
using LimpShared.Models.Authentication.Types;
using Microsoft.AspNetCore.SignalR.Client;

namespace Ethachat.Client.Services.AuthenticationService;

public class AuthenticationManager : IAuthenticationHandler, IAuthenticationManager
{
    private readonly IJwtHandler _jwtHandler;
    private readonly IWebAuthnHandler _webAuthnHandler;

    public AuthenticationManager(IJwtHandler jwtHandler, IWebAuthnHandler webAuthnHandler)
    {
        _jwtHandler = jwtHandler;
        _webAuthnHandler = webAuthnHandler;
    }

    private async Task<IAuthenticationHandler> GetAvailableHandlerAsync()
    {
        if (await _jwtHandler.IsSetToUseAsync())
            return _jwtHandler;

        if (await _webAuthnHandler.IsSetToUseAsync())
            return _webAuthnHandler;

        throw new ArgumentException("No active authentication handlers.");
    }

    public async Task<CredentialsDTO> GetCredentialsDto()
    {
        var handler = await GetAvailableHandlerAsync();
        return await handler.GetCredentialsDto();
    }

    public async Task<ICredentials> GetCredentials()
    {
        var handler = await GetAvailableHandlerAsync();
        return await handler.GetCredentials();
    }

    public async Task<AuthenticationType?> GetAuthenticationTypeAsync()
    {
        var handler = await GetAvailableHandlerAsync();
        return await handler.GetAuthenticationTypeAsync();
    }

    public async Task<string> GetRefreshCredential()
    {
        var handler = await GetAvailableHandlerAsync();
        return await handler.GetRefreshCredential();
    }

    public async Task<string> GetAccessCredential()
    {
        var handler = await GetAvailableHandlerAsync();
        return await handler.GetAccessCredential();
    }

    public async Task<string> GetUsernameAsync()
    {
        var handler = await GetAvailableHandlerAsync();
        return await handler.GetUsernameAsync();
    }

    public async Task<bool> IsSetToUseAsync()
    {
        try
        {
            var handler = await GetAvailableHandlerAsync();
            return await handler.IsSetToUseAsync();
        }
        catch
        {
            return false;
        }
    }

    public async Task TriggerCredentialsValidation(HubConnection hubConnection)
    {
        var handler = await GetAvailableHandlerAsync();
        await handler.TriggerCredentialsValidation(hubConnection);
    }

    public async Task UpdateCredentials(ICredentials newCredentials)
    {
        var handler = await GetAvailableHandlerAsync();
        await handler.UpdateCredentials(newCredentials);
    }

    public async Task ExecutePostCredentialsValidation(AuthResult result, HubConnection hubConnection)
    {
        var handler = await GetAvailableHandlerAsync();
        await handler.ExecutePostCredentialsValidation(result, hubConnection);
    }
}