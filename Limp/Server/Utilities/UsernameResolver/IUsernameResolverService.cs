using EthachatShared.Models.Authentication.Models;
using EthachatShared.Models.Authentication.Models.Credentials.CredentialsDTO;

namespace Ethachat.Server.Utilities.UsernameResolver;

public interface IUsernameResolverService
{
    Task<AuthResult> GetUsernameAsync(CredentialsDTO credentialsDto);
}