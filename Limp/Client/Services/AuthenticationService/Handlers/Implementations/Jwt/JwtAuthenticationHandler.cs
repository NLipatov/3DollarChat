using System.IdentityModel.Tokens.Jwt;
using Limp.Client.Services.LocalStorageService;
using Limp.Client.Services.UserAgentService;
using LimpShared.Models.Authentication.Models;
using LimpShared.Models.Authentication.Models.Credentials;
using LimpShared.Models.Authentication.Models.Credentials.CredentialsDTO;
using LimpShared.Models.Authentication.Models.Credentials.Implementation;
using LimpShared.Models.Authentication.Types;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace Limp.Client.Services.AuthenticationService.Handlers.Implementations.Jwt;

public class JwtAuthenticationHandler : IJwtHandler
{
    private readonly ILocalStorageService _localStorageService;
    private readonly IUserAgentService _userAgentService;
    private readonly IJSRuntime _jsRuntime;

    private async Task<string?> GetAccessTokenAsync() =>
        await _localStorageService.ReadPropertyAsync("access-token");

    private async Task<string?> GetRefreshTokenAsync() =>
        await _localStorageService.ReadPropertyAsync("refresh-token");

    public JwtAuthenticationHandler(ILocalStorageService localStorageService, IUserAgentService userAgentService, IJSRuntime jsRuntime)
    {
        _localStorageService = localStorageService;
        _userAgentService = userAgentService;
        _jsRuntime = jsRuntime;
    }

    public async Task<ICredentials> GetCredentials()
    {
        return new JwtPair()
        {
            AccessToken = await GetAccessCredential(),
            RefreshToken = new RefreshToken()
            {
                Token = await GetRefreshCredential()
            }
        };
    }

    public async Task<AuthenticationType?> GetAuthenticationTypeAsync()
    {
        return AuthenticationType.JwtToken;
    }

    public async Task<string> GetRefreshCredential() => await GetRefreshTokenAsync() ?? string.Empty;

    public async Task<string> GetAccessCredential() => await GetAccessTokenAsync() ?? string.Empty;

    public async Task<string> GetUsernameAsync()
    {
        var accessToken = await GetAccessTokenAsync();
        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.ReadToken(accessToken) as JwtSecurityToken;

        return securityToken?.Claims.FirstOrDefault(claim => claim.Type == "unique_name")?.Value ?? string.Empty;
    }

    public async Task<bool> IsSetToUseAsync()
    {
        JwtPair jWtPair = await GetJwtPairAsync();
        return !string.IsNullOrWhiteSpace(jWtPair.AccessToken) &&
               !string.IsNullOrWhiteSpace(jWtPair.RefreshToken.Token);
    }

    public async Task TriggerCredentialsValidation(HubConnection hubConnection)
    {
        JwtPair jWtPair = await GetJwtPairAsync();
        var isCredentialsBeingRefreshed = await TryRefreshCredentialsAsync(hubConnection);

        if (!isCredentialsBeingRefreshed)
        {
            await hubConnection.SendAsync("ValidateCredentials", new CredentialsDTO{JwtPair = jWtPair});
        }
    }

    public async Task UpdateCredentials(ICredentials newCredentials)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "access-token", (newCredentials as JwtPair)!.AccessToken);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "refresh-token", (newCredentials as JwtPair)!.RefreshToken.Token);
    }

    private async Task<JwtPair> GetJwtPairAsync()
    {
        var accessToken = await GetAccessTokenAsync();
        var refreshToken = await GetRefreshTokenAsync();

        if (string.IsNullOrWhiteSpace(accessToken)
            ||
            string.IsNullOrWhiteSpace(refreshToken))
        {
            return new JwtPair
            {
                AccessToken = string.Empty,
                RefreshToken = new RefreshToken
                {
                    Token = string.Empty
                }
            };
        }

        return new JwtPair
        {
            AccessToken = accessToken,
            RefreshToken = new RefreshToken
            {
                Token = refreshToken
            }
        };
    }

    /// <summary>
    /// Checks if credentials are outdated and updates it
    /// </summary>
    /// <returns>bool which determins if credentials refresh is in progress now</returns>
    public async Task<bool> TryRefreshCredentialsAsync(HubConnection hubConnection)
    {
        JwtPair? jwtPair = await GetCredentials() as JwtPair;
        if (jwtPair is not null)
        {
            if (await IsAccessTokenExpiredAsync())
            {
                var userAgentInformation = await _userAgentService.GetUserAgentInformation();

                await hubConnection.SendAsync("RefreshCredentials", new CredentialsDTO {JwtPair = jwtPair} );

                return true;
            }
        }

        return false;
    }

    private async Task<bool> IsAccessTokenExpiredAsync()
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        var securityToken = tokenHandler.ReadToken(await GetAccessCredential()) as JwtSecurityToken;

        if (securityToken?.ValidTo is null)
            return false;

        return securityToken.ValidTo <= DateTime.UtcNow;
    }
}