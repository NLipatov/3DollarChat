using EthachatShared.Models.Authentication.Types;

namespace EthachatShared.Models.Authentication.Models.Credentials;

public interface ICredentials
{
    public AuthenticationType Type { get; }
}