using System.IdentityModel.Tokens.Jwt;

namespace Limp.Client.Utilities
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
    }
}
