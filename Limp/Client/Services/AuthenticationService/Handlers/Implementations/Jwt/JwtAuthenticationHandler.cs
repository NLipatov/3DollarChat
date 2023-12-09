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

    public async Task<CredentialsDTO> GetCredentialsDto()
    {
        return new CredentialsDTO()
        {
            JwtPair = await GetCredentials() as JwtPair
        };
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

        var usernameClaimKey = "unique_name";
        return securityToken?.Claims.FirstOrDefault(claim => claim.Type == usernameClaimKey)?.Value 
               ?? throw new ApplicationException($"Exception:" +
                                                 $"{nameof(JwtAuthenticationHandler)}.{nameof(GetUsernameAsync)}:" +
                                                 $"Could not get a '{usernameClaimKey}' claim value.");
    }

    public async Task<bool> IsSetToUseAsync()
    {
        JwtPair jWtPair = await GetJwtPairAsync();
        return !string.IsNullOrWhiteSpace(jWtPair.AccessToken) &&
               !string.IsNullOrWhiteSpace(jWtPair.RefreshToken.Token);
    }

    public async Task TriggerCredentialsValidation(HubConnection hubConnection, Func<int, Task>? revalidationCallback = null)
    {
        JwtPair jWtPair = await GetJwtPairAsync();
        var isCredentialsBeingRefreshed = await TryRefreshCredentialsAsync(hubConnection, revalidationCallback);

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
    private async Task<bool> TryRefreshCredentialsAsync(HubConnection hubConnection, Func<int, Task>? revalidationCallback = null)
    {
        JwtPair? jwtPair = await GetCredentials() as JwtPair;
        if (jwtPair is not null)
        {
            var tokenTtl = await GetTokenTimeToLiveAsync();
            if (tokenTtl > 0)
            {
                revalidationCallback?.Invoke(tokenTtl - 10);
                
                var userAgentInformation = await _userAgentService.GetUserAgentInformation();

                await hubConnection.SendAsync("RefreshCredentials", new CredentialsDTO {JwtPair = jwtPair} );

                return true;
            }
        }

        return false;
    }

    private async Task<int> GetTokenTimeToLiveAsync()
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        var securityToken = tokenHandler.ReadToken(await GetAccessCredential()) as JwtSecurityToken;

        if (securityToken?.ValidTo is null)
            return 0;

        return (int)(securityToken.ValidTo - DateTime.UtcNow).TotalSeconds;
    }
}