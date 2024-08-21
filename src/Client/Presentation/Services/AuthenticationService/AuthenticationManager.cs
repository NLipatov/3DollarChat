using Client.Application.Gateway;
using Ethachat.Client.Services.AuthenticationService.Handlers;
using Ethachat.Client.Services.AuthenticationService.Handlers.Implementations.Jwt;
using Ethachat.Client.Services.AuthenticationService.Handlers.Implementations.WebAuthn;
using EthachatShared.Models.Authentication.Models;
using EthachatShared.Models.Authentication.Models.Credentials;
using EthachatShared.Models.Authentication.Models.Credentials.CredentialsDTO;
using EthachatShared.Models.Authentication.Types;

namespace Ethachat.Client.Services.AuthenticationService;

public class AuthenticationManager(IJwtHandler jwtHandler, IWebAuthnHandler webAuthnHandler)
    : IAuthenticationHandler, IAuthenticationManager
{
    private async Task<IAuthenticationHandler?> GetAvailableHandlerAsync()
    {
        if (await jwtHandler.IsSetToUseAsync())
            return jwtHandler;

        if (await webAuthnHandler.IsSetToUseAsync())
            return webAuthnHandler;
        
        return null;
    }

    public async Task<CredentialsDTO> GetCredentialsDto()
    {
        var handler = await GetAvailableHandlerAsync() ?? throw new NullReferenceException();
        return await handler.GetCredentialsDto();
    }

    public async Task<ICredentials> GetCredentials()
    {
        var handler = await GetAvailableHandlerAsync() ?? throw new NullReferenceException();
        return await handler.GetCredentials();
    }

    public async Task<AuthenticationType?> GetAuthenticationTypeAsync()
    {
        var handler = await GetAvailableHandlerAsync() ?? throw new NullReferenceException();
        return await handler.GetAuthenticationTypeAsync();
    }

    public async Task<string> GetRefreshCredential()
    {
        var handler = await GetAvailableHandlerAsync() ?? throw new NullReferenceException();
        return await handler.GetRefreshCredential();
    }

    public async Task<string> GetAccessCredential()
    {
        var handler = await GetAvailableHandlerAsync() ?? throw new NullReferenceException();
        return await handler.GetAccessCredential();
    }

    public async Task<string> GetUsernameAsync()
    {
        var handler = await GetAvailableHandlerAsync() ?? throw new NullReferenceException();
        return await handler.GetUsernameAsync();
    }

    public async Task<bool> IsSetToUseAsync()
    {
        var handler = await GetAvailableHandlerAsync();
        
        if (handler is null) 
            return false;
        
        return await handler.IsSetToUseAsync();
    }

    public async Task TriggerCredentialsValidation(IGateway gateway)
    {
        var handler = await GetAvailableHandlerAsync() ?? throw new NullReferenceException();
        await handler.TriggerCredentialsValidation(gateway);
    }

    public async Task UpdateCredentials(ICredentials newCredentials)
    {
        var handler = await GetAvailableHandlerAsync() ?? throw new NullReferenceException();
        await handler.UpdateCredentials(newCredentials);
    }

    public async Task ExecutePostCredentialsValidation(AuthResult result, IGateway gateway)
    {
        var handler = await GetAvailableHandlerAsync() ?? throw new NullReferenceException();
        await handler.ExecutePostCredentialsValidation(result, gateway);
    }
}