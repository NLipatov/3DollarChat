using Ethachat.Client.Services.AuthenticationService.Handlers;
using Ethachat.Client.Services.AuthenticationService.Handlers.Implementations.Jwt;
using Ethachat.Client.Services.AuthenticationService.Handlers.Implementations.WebAuthn;
using EthachatShared.Models.Authentication.Models;
using EthachatShared.Models.Authentication.Models.Credentials;
using EthachatShared.Models.Authentication.Models.Credentials.CredentialsDTO;
using EthachatShared.Models.Authentication.Types;
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

    private async Task<IAuthenticationHandler?> GetAvailableHandlerAsync()
    {
        if (await _jwtHandler.IsSetToUseAsync())
            return _jwtHandler;

        if (await _webAuthnHandler.IsSetToUseAsync())
            return _webAuthnHandler;
        
        return null;
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
        var handler = await GetAvailableHandlerAsync();
        
        if (handler is null) 
            return false;
        
        return await handler.IsSetToUseAsync();
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