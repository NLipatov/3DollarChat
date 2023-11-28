using LimpShared.Models.Authentication.Models.Credentials.CredentialsDTO;

namespace Limp.Server.Utilities.UsernameResolver;

public interface IUsernameResolverService
{
    Task<string> GetUsernameAsync(CredentialsDTO credentialsDto);
}