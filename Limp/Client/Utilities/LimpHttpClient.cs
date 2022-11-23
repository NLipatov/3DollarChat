using LimpShared.Authentification;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text.Json;
using System.Text;
using AuthAPI.DTOs.User;
using Microsoft.AspNetCore.Mvc;

namespace Limp.Client.Utilities
{
    public class LimpHttpClient : ILimpHttpClient
    {
        private static IConfiguration _configuration;
        public LimpHttpClient(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task<JWTPair?> GetJWTPairAsync(UserDTO userDTO)
        {
            var content = new StringContent(JsonSerializer.Serialize(userDTO), Encoding.UTF8, "application/json");

            HttpClient client = new();
            var response = await client.PostAsync(_configuration["AuthAutority:Address"] + _configuration["AuthAutority:Endpoints:Get-Token"],
            content);

            return JsonSerializer.Deserialize<JWTPair>(await response.Content.ReadAsStringAsync());
        }

        public async Task<string> GetUserNameFromAccessTokenAsync(string accessToken)
        {
            var requestUrl = $"{_configuration["AuthAutority:Address"]}{_configuration["AuthAutority:Endpoints:GetUserName"]}?accessToken={accessToken.ToString()}";

            HttpClient client = new();

            var response = await client.GetAsync(requestUrl);

            return await response.Content.ReadAsStringAsync();
        }
    }
}
