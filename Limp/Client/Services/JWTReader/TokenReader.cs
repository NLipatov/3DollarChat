using System.IdentityModel.Tokens.Jwt;

namespace Limp.Client.Services.JWTReader
{
    public static class TokenReader
    {
        public static bool IsTokenReadable(string accessToken)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                tokenHandler.ReadJwtToken(accessToken);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }
        public static bool HasAccessTokenExpired(string accessToken)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var securityToken = tokenHandler.ReadToken(accessToken) as JwtSecurityToken;
            
            if (securityToken?.ValidTo == null)
                throw new ArgumentException("Access token is not valid");

            var now = DateTime.UtcNow;

            return securityToken.ValidTo <= now;
        }

        public static string GetUsernameFromAccessToken(string? accessToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ApplicationException($"Passed in parameter {nameof(accessToken)} was empty string or null.");

            string? usernameFromAccessToken = ReadUsernameFromAccessToken(accessToken);
            if (string.IsNullOrWhiteSpace(usernameFromAccessToken))
                throw new ApplicationException("Could read username from access-token");

            return usernameFromAccessToken;
        }

        private static string? ReadUsernameFromAccessToken(string accessToken)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.ReadToken(accessToken) as JwtSecurityToken;

            return securityToken?.Claims.FirstOrDefault(claim => claim.Type == "unique_name")?.Value;
        }
    }
}
