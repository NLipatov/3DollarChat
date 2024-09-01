using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using Client.Application.Gateway;
using Ethachat.Client.Extensions;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;
using Ethachat.Client.Services.LocalStorageService;
using EthachatShared.Models.Authentication.Models;
using EthachatShared.Models.Authentication.Models.Credentials;
using EthachatShared.Models.Authentication.Models.Credentials.CredentialsDTO;
using EthachatShared.Models.Authentication.Models.Credentials.Implementation;
using EthachatShared.Models.Authentication.Types;
using EthachatShared.Models.Message;
using Microsoft.JSInterop;

namespace Ethachat.Client.Services.Authentication.Handlers.Implementations.Jwt;

public class JwtAuthenticationHandler(
    ILocalStorageService localStorageService,
    IJSRuntime jsRuntime,
    ICallbackExecutor callbackExecutor)
    : IJwtHandler
{
    private readonly ConcurrentDictionary<string, DateTime> _tokenCache = [];

    private async Task<string?> GetAccessTokenAsync() =>
        await localStorageService.ReadPropertyAsync("access-token");

    private async Task<string?> GetRefreshTokenAsync() =>
        await localStorageService.ReadPropertyAsync("refresh-token");

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

    public Task<AuthenticationType?> GetAuthenticationTypeAsync()
    {
        return Task.FromResult<AuthenticationType?>(AuthenticationType.JwtToken);
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

    public async Task TriggerCredentialsValidation(IGateway gateway)
    {
        JwtPair jWtPair = await GetJwtPairAsync();

        if (await TryUseCachedCredentialsAsync(jWtPair))
            return;

        var isCredentialsBeingRefreshed = await TryRefreshCredentialsAsync(gateway);
        if (!isCredentialsBeingRefreshed)
        {
            await gateway.TransferAsync(new ClientToServerData
            {
                EventName = "ValidateCredentials",
                Data = await new CredentialsDTO { JwtPair = jWtPair }.SerializeAsync(),
                Type = typeof(CredentialsDTO)
            });
        }
    }

    private async Task<bool> TryUseCachedCredentialsAsync(JwtPair jwtPair)
    {
        if (_tokenCache.TryGetValue(jwtPair.AccessToken, out var validTo))
        {
            if (validTo - TimeSpan.FromMinutes(1) > DateTime.UtcNow)
            {
                callbackExecutor.ExecuteSubscriptionsByName(new AuthResult
                {
                    JwtPair = jwtPair,
                    Result = AuthResultType.Success,
                    Message = "Cached Token Validation Result"
                }, "OnValidateCredentials");

                return true;
            }
        }

        _tokenCache.Clear();
        _tokenCache.TryAdd(jwtPair.AccessToken, await GetTokenValidToAsync());

        return false;
    }

    public async Task UpdateCredentials(ICredentials newCredentials)
    {
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", "access-token",
            (newCredentials as JwtPair)!.AccessToken);
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", "refresh-token",
            (newCredentials as JwtPair)!.RefreshToken.Token);
    }

    public Task ExecutePostCredentialsValidation(AuthResult result, IGateway gateway)
    {
        return Task.CompletedTask;
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
    private async Task<bool> TryRefreshCredentialsAsync(IGateway gateway)
    {
        JwtPair? jwtPair = await GetCredentials() as JwtPair;
        if (jwtPair is not null)
        {
            var tokenTtl = await GetTokenTimeToLiveSecondsAsync();
            if (tokenTtl <= 60)
            {
                await gateway.TransferAsync(new ClientToServerData
                {
                    EventName = "RefreshCredentials",
                    Data = await new CredentialsDTO { JwtPair = jwtPair }.SerializeAsync(),
                    Type = typeof(CredentialsDTO),
                });

                return true;
            }
        }

        return false;
    }

    private async Task<int> GetTokenTimeToLiveSecondsAsync()
    {
        var validTo = await GetTokenValidToAsync();

        return (int)(validTo - DateTime.UtcNow).TotalSeconds;
    }

    private async Task<DateTime> GetTokenValidToAsync()
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        var securityToken = tokenHandler.ReadToken(await GetAccessCredential()) as JwtSecurityToken;

        return securityToken?.ValidTo ?? DateTime.MinValue;
    }
}