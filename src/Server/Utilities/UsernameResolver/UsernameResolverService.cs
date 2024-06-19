using System.IdentityModel.Tokens.Jwt;
using Ethachat.Client.Services.AuthenticationService;
using Ethachat.Server.Utilities.HttpMessaging;
using EthachatShared.Models.Authentication.Models;
using EthachatShared.Models.Authentication.Models.Credentials.CredentialsDTO;

namespace Ethachat.Server.Utilities.UsernameResolver;

public class UsernameResolverService : IUsernameResolverService
{
    private readonly IServerHttpClient _serverHttpClient;
    private readonly IAuthenticationManager _authenticationManager;

    public UsernameResolverService(IServerHttpClient serverHttpClient)
    {
        _serverHttpClient = serverHttpClient;
    }

    public async Task<AuthResult> GetUsernameAsync(CredentialsDTO credentialsDto)
    {
        var result = await _serverHttpClient.GetUsernameByCredentials(credentialsDto);
        return result;
    }

    private bool IsTokenReadable(string accessToken)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            tokenHandler.ReadJwtToken(accessToken);
            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }
}