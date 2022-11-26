using AuthAPI.DTOs.User;
using Limp.Shared.Models.Login;
using LimpShared.Authentification;
using System.Text;
using System.Text.Json;

namespace Limp.Server.Utilities.HttpMessaging
{
    public class ServerHttpClient : IServerHttpClient
    {
        private static IConfiguration _configuration;
        public ServerHttpClient(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task<LogInResult> GetJWTPairAsync(UserDTO userDTO)
        {
            var content = new StringContent(JsonSerializer.Serialize(userDTO), Encoding.UTF8, "application/json");

            HttpClient client = new();
            var response = await client.PostAsync(_configuration["AuthAutority:Address"] + _configuration["AuthAutority:Endpoints:Get-Token"], content);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                return new LogInResult
                {
                    Message = await response.Content.ReadAsStringAsync(),
                    Result = LogInStatus.Fail,
                };
            }

            var responseContent = await response.Content.ReadAsStringAsync();

            JWTPair jwtPair = JsonSerializer.Deserialize<JWTPair>(responseContent);

            return new LogInResult
            {
                Result = LogInStatus.Success,
                JWTPair = jwtPair,
            };
        }

        public async Task<TokenRelatedOperationResult> GetUserNameFromAccessTokenAsync(string accessToken)
        {
            var requestUrl = $"{_configuration["AuthAutority:Address"]}{_configuration["AuthAutority:Endpoints:GetUserName"]}?accessToken={accessToken}";

            HttpClient client = new();

            var response = await client.GetAsync(requestUrl);

            TokenRelatedOperationResult result = JsonSerializer.Deserialize<TokenRelatedOperationResult>(await response.Content.ReadAsStringAsync());

            return result;
        }

        public async Task<LogInResult> ExplicitJWTPairRefresh(RefreshToken refreshToken)
        {
            var content = new StringContent(JsonSerializer.Serialize(refreshToken), Encoding.UTF8, "application/json");

            var requestUrl = $"{_configuration["AuthAutority:Address"]}{_configuration["AuthAutority:Endpoints:ExplicitRefreshTokens"]}";

            HttpClient client = new();

            var response = await client.PostAsync(requestUrl, content);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                return new LogInResult
                {
                    Message = await response.Content.ReadAsStringAsync(),
                    Result = LogInStatus.Fail,
                };
            }

            var responseContent = await response.Content.ReadAsStringAsync();

            JWTPair jwtPair = JsonSerializer.Deserialize<JWTPair>(responseContent);

            return new LogInResult()
            {
                Result = LogInStatus.Success,
                JWTPair = jwtPair,
            };
        }

        public async Task<bool> IsAccessTokenValid(string accessToken)
        {
            HttpClient client = new();

            var requestUrl = $"{_configuration["AuthAutority:Address"]}{_configuration["AuthAutority:Endpoints:ValidateAccessToken"]}?accesstoken={accessToken}";

            var response = await client.GetAsync(requestUrl);

            return response.StatusCode == System.Net.HttpStatusCode.OK;
        }
    }
}
