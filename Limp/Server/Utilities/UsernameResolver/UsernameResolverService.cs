using System.IdentityModel.Tokens.Jwt;
using Ethachat.Client.Services.AuthenticationService;
using EthachatShared.Models.Authentication.Models.Credentials.CredentialsDTO;
using Limp.Server.Utilities.HttpMessaging;
using EthachatShared.Models.Authentication.Models;

namespace Limp.Server.Utilities.UsernameResolver;

public class UsernameResolverService : IUsernameResolverService
{
    private readonly IServerHttpClient _serverHttpClient;
    private readonly IAuthenticationManager _authenticationManager;

    public UsernameResolverService(IServerHttpClient serverHttpClient)
    {
        _serverHttpClient = serverHttpClient;
    }

    public async Task<string> GetUsernameAsync(CredentialsDTO credentialsDto)
    {
        var result = await _serverHttpClient.GetUsernameByCredentials(credentialsDto);
        return result.Result is AuthResultType.Success ? result.Message : string.Empty;
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