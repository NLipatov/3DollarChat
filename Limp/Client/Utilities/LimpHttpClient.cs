using AuthAPI.DTOs.User;
using Limp.Shared.Models.Login;
using LimpShared.Authentification;
using System.Text;
using System.Text.Json;

namespace Limp.Client.Utilities
{
    public class LimpHttpClient : ILimpHttpClient
    {
        private static IConfiguration _configuration;
        public LimpHttpClient(IConfiguration configuration)
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

            var jwtPair = JsonSerializer.Deserialize<JWTPair>(await response.Content.ReadAsStringAsync());

            return new LogInResult
            {
                Result = LogInStatus.Success,
                JWTPair = jwtPair,
            };
        }

        public async Task<string> GetUserNameFromAccessTokenAsync(string accessToken)
        {
            var requestUrl = $"{_configuration["AuthAutority:Address"]}{_configuration["AuthAutority:Endpoints:GetUserName"]}?accessToken={accessToken}";

            HttpClient client = new();

            var response = await client.GetAsync(requestUrl);

            return await response.Content.ReadAsStringAsync();
        }
    }
}
