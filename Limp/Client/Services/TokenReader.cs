using LimpShared.Encryption;
using System.IdentityModel.Tokens.Jwt;

namespace Limp.Client.Services
{
    public static class TokenReader
    {
        public static bool HasAccessTokenExpired(string accessToken)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var securityToken = tokenHandler.ReadToken(accessToken) as JwtSecurityToken;

            if (securityToken?.ValidTo == null)
                throw new ArgumentException("Access token is not valid");

            var now = DateTime.UtcNow;

            return securityToken.ValidTo <= now;
        }

        public static string GetUsername(string accessToken)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.ReadToken(accessToken) as JwtSecurityToken;

            return securityToken?.Claims.FirstOrDefault(claim => claim.Type == "unique_name")?.Value ?? "Anonymous";
        }

        public static Key GetPublicKey(string accessToken, string contactName)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.ReadToken(accessToken) as JwtSecurityToken;

            return new Key
            {
                Format = KeyFormat.PEM_SPKI,
                Type = KeyType.RSAPublic,
                Value = securityToken?.Claims?.FirstOrDefault(claim => claim.Type == "RSA Public Key")?.Value,
                Contact = contactName,
            };
        }
    }
}
