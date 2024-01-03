using EthachatShared.Models.Authentication.Models;
using EthachatShared.Models.Authentication.Models.Credentials.CredentialsDTO;

namespace Limp.Server.Utilities.UsernameResolver;

public interface IUsernameResolverService
{
    Task<AuthResult> GetUsernameAsync(CredentialsDTO credentialsDto);
}