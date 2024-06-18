using EthachatShared.Models.Authentication.Types;

namespace EthachatShared.Models.Authentication.Models.Credentials.Implementation;

public class WebAuthnPair : ICredentials
{
    public string CredentialId { get; set; }
    public uint Counter { get; set; }
    public AuthenticationType Type => AuthenticationType.WebAuthn;
}