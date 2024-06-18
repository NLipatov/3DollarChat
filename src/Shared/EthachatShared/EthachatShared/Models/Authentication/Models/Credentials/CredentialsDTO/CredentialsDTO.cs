using EthachatShared.Models.Authentication.Models.Credentials.Implementation;

namespace EthachatShared.Models.Authentication.Models.Credentials.CredentialsDTO;

public record CredentialsDTO
{
    public JwtPair? JwtPair { get; set; }
    public WebAuthnPair? WebAuthnPair { get; set; }
}