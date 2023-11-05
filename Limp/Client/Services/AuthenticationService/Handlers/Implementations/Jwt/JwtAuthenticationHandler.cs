using System.IdentityModel.Tokens.Jwt;
using Limp.Client.Services.LocalStorageService;
using LimpShared.Models.Authentication.Models;
using LimpShared.Models.Authentication.Models.Credentials;
using LimpShared.Models.Authentication.Models.Credentials.Implementation;
using LimpShared.Models.Authentication.Types;
using Microsoft.AspNetCore.SignalR.Client;

namespace Limp.Client.Services.AuthenticationService.Handlers.Implementations.Jwt;

public class JwtAuthenticationHandler : IJwtHandler
{
    private readonly ILocalStorageService _localStorageService;

    private async Task<string?> GetAccessTokenAsync() =>
        await _localStorageService.ReadPropertyAsync("access-token");

    private async Task<string?> GetRefreshTokenAsync() =>
        await _localStorageService.ReadPropertyAsync("refresh-token");

    public JwtAuthenticationHandler(ILocalStorageService localStorageService)
    {
        _localStorageService = localStorageService;
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
        JwtPair? jWtPair = await GetJwtPairAsync();
        await hubConnection.SendAsync("IsTokenValid", jWtPair.AccessToken ?? string.Empty);
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
}