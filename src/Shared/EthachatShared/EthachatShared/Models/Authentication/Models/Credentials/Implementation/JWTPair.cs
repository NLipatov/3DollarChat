#nullable disable

using EthachatShared.Models.Authentication.Types;

namespace EthachatShared.Models.Authentication.Models.Credentials.Implementation
{
    public class JwtPair : ICredentials
    {
        public string AccessToken { get; set; }
        public RefreshToken RefreshToken { get; set; }
        public AuthenticationType Type => AuthenticationType.JwtToken;
    }
}
