using AuthAPI.DTOs.User;
using ClientServerCommon.Models.Login;
using LimpShared.Authentification;
using LimpShared.DTOs.PublicKey;
using LimpShared.DTOs.User;
using LimpShared.ResultTypeEnum;
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

        public async Task<AuthResult> Register(UserDTO userDTO)
        {
            var content = new StringContent(JsonSerializer.Serialize(userDTO), Encoding.UTF8, "application/json");

            HttpClient client = new();

            string url = _configuration["AuthAutority:Address"] + _configuration["AuthAutority:Endpoints:Register"];

            var response = await client.PostAsync(url, content);

            var serializedResponse = await response.Content.ReadAsStringAsync();
            UserOperationResult? deserializedResponse = JsonSerializer.Deserialize<UserOperationResult>(serializedResponse);

            if (deserializedResponse == null)
            {
                throw new ApplicationException("Could not get response from AuthAPI");
            }

            return new AuthResult
            {
                Message = deserializedResponse.SystemMessage,
                Result = deserializedResponse.ResultType == OperationResultType.Success ? AuthResultType.Success : AuthResultType.Fail,
            };
        }

        public async Task<AuthResult> GetJWTPairAsync(UserDTO userDTO)
        {
            var content = new StringContent(JsonSerializer.Serialize(userDTO), Encoding.UTF8, "application/json");

            HttpClient client = new();
            var response = await client.PostAsync(_configuration["AuthAutority:Address"] + _configuration["AuthAutority:Endpoints:Get-Token"], content);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                return new AuthResult
                {
                    Message = await response.Content.ReadAsStringAsync(),
                    Result = AuthResultType.Fail,
                };
            }

            var responseContent = await response.Content.ReadAsStringAsync();

            JWTPair jwtPair = JsonSerializer.Deserialize<JWTPair>(responseContent);

            return new AuthResult
            {
                Result = AuthResultType.Success,
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

        public async Task<AuthResult> ExplicitJWTPairRefresh(RefreshToken refreshToken)
        {
            var content = new StringContent(JsonSerializer.Serialize(refreshToken), Encoding.UTF8, "application/json");

            var requestUrl = $"{_configuration["AuthAutority:Address"]}{_configuration["AuthAutority:Endpoints:ExplicitRefreshTokens"]}";

            HttpClient client = new();

            var response = await client.PostAsync(requestUrl, content);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                return new AuthResult
                {
                    Message = await response.Content.ReadAsStringAsync(),
                    Result = AuthResultType.Fail,
                };
            }

            var responseContent = await response.Content.ReadAsStringAsync();

            JWTPair jwtPair = JsonSerializer.Deserialize<JWTPair>(responseContent);

            return new AuthResult()
            {
                Result = AuthResultType.Success,
                JWTPair = jwtPair,
            };
        }

        public async Task<bool> IsAccessTokenValid(string accessToken)
        {
            HttpClient client = new();

            var requestUrl = $"{_configuration["AuthAutority:Address"]}{_configuration["AuthAutority:Endpoints:ValidateAccessToken"]}?accesstoken={accessToken}";

            var response = await client.GetAsync(requestUrl);

            var responseContent = await response.Content.ReadAsStringAsync();

            TokenRelatedOperationResult? result = JsonSerializer.Deserialize<TokenRelatedOperationResult>(responseContent);

            if (result == null)
                return false;

            return result.ResultType == OperationResultType.Success;
        }

        public async Task SetRSAPublicKey(string PEMEncodedRSAPublicKey, string username)
        {
            var requestUrl = $"{_configuration["AuthAutority:Address"]}{_configuration["AuthAutority:Endpoints:SetRSAPublicKey"]}";

            using(HttpClient client = new())
            {
                await client.PostAsJsonAsync(requestUrl, new PublicKeyDTO { Username = username, Key = PEMEncodedRSAPublicKey});
            }
        }
    }
}
