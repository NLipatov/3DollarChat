namespace Limp.Server.Utilities.UsernameResolver;

public interface IUsernameResolverService
{
    Task<string> GetUsernameAsync(string accessToken);
}