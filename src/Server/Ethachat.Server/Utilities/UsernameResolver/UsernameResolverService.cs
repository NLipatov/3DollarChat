using Ethachat.Server.Utilities.HttpMessaging;
using EthachatShared.Models.Authentication.Models;
using EthachatShared.Models.Authentication.Models.Credentials.CredentialsDTO;

namespace Ethachat.Server.Utilities.UsernameResolver;

public class UsernameResolverService(IServerHttpClient serverHttpClient) : IUsernameResolverService
{
    public async Task<AuthResult> GetUsernameAsync(CredentialsDTO credentialsDto)
        => await serverHttpClient.GetUsernameByCredentials(credentialsDto);
}