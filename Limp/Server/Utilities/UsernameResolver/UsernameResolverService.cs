using System.IdentityModel.Tokens.Jwt;
using Limp.Server.Utilities.HttpMessaging;

namespace Limp.Server.Utilities.UsernameResolver;

public class UsernameResolverService : IUsernameResolverService
{
    private readonly IServerHttpClient _serverHttpClient;

    public UsernameResolverService(IServerHttpClient serverHttpClient)
    {
        _serverHttpClient = serverHttpClient;
    }

    public async Task<string> GetUsernameAsync(string accessToken)
    {
        if (IsTokenReadable(accessToken))
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.ReadToken(accessToken) as JwtSecurityToken;

            return securityToken?.Claims.FirstOrDefault(claim => claim.Type == "unique_name")?.Value ?? string.Empty;
        }

        return await _serverHttpClient.GetUsernameByCredentialId(accessToken);
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