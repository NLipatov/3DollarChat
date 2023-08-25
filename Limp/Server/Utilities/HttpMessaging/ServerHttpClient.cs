using LimpShared.Models.Authentication.Models;
using LimpShared.Models.Authentication.Models.AuthenticatedUserRepresentation.PublicKey;
using LimpShared.Models.Authentication.Models.UserAuthentication;
using LimpShared.Models.AuthenticationModels.ResultTypeEnum;
using LimpShared.Models.Users;
using LimpShared.Models.WebPushNotification;
using System.Net;
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

        public async Task<AuthResult> Register(UserAuthentication userDTO)
        {
            var content = new StringContent(JsonSerializer.Serialize(userDTO), Encoding.UTF8, "application/json");

            HttpClient client = new();

            string url = _configuration["AuthAutority:Address"] + _configuration["AuthAutority:Endpoints:Register"];

            var response = await client.PostAsync(url, content);

            var serializedResponse = await response.Content.ReadAsStringAsync();
            UserAuthenticationOperationResult? deserializedResponse = JsonSerializer.Deserialize<UserAuthenticationOperationResult>(serializedResponse);

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

        public async Task<AuthResult> GetJWTPairAsync(UserAuthentication userDTO)
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

        public async Task<AuthResult> ExplicitJWTPairRefresh(RefreshTokenDTO dto)
        {
            var content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");

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

        public async Task PostAnRSAPublic(PublicKeyDTO publicKeyDTO)
        {
            var requestUrl = $"{_configuration["AuthAutority:Address"]}{_configuration["AuthAutority:Endpoints:RSAPublic"]}";

            using(HttpClient client = new())
            {
                await client.PostAsJsonAsync(requestUrl, new PublicKeyDTO { Username = publicKeyDTO.Username, Key = publicKeyDTO.Key});
            }
        }

        public async Task<string?> GetAnRSAPublicKey(string username)
        {
            var requestUrl = $"{_configuration["AuthAutority:Address"]}{_configuration["AuthAutority:Endpoints:RSAPublic"]}/{username}";

        using(HttpClient client = new())
            {
                var response = await client.GetAsync(requestUrl);
                var pemEncodedKey = await response.Content.ReadAsStringAsync();
                return pemEncodedKey;
            }
        }

        public async Task AddUserWebPushSubscribtion(NotificationSubscriptionDTO subscriptionDTO)
        {
            var requestUrl = $"{_configuration["AuthAutority:Address"]}{_configuration["AuthAutority:Endpoints:SubscribeToWebPush"]}";

            using (HttpClient client = new())
            {
                await client.PutAsJsonAsync(requestUrl, subscriptionDTO);
            }
        }

        public async Task<NotificationSubscriptionDTO[]> GetUserWebPushSubscriptionsByAccessToken(string username)
        {
            var requestUrl = $"{_configuration["AuthAutority:Address"]}{_configuration["AuthAutority:Endpoints:GetNotificationsByUserId"]}/{username}";

            using (HttpClient client = new())
            {
                var response = await client.GetAsync(requestUrl);
                var serializedSubscriptions = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<NotificationSubscriptionDTO[]>(serializedSubscriptions, options: new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new NotificationSubscriptionDTO[0];
            }
        }

        public async Task RemoveUserWebPushSubscriptions(NotificationSubscriptionDTO[] subscriptionsToRemove)
        {
            var requestUrl = $"{_configuration["AuthAutority:Address"]}{_configuration["AuthAutority:Endpoints:RemoveWebPushSubscriptions"]}";

            using (HttpClient client = new())
            {
                var response = await client.PatchAsJsonAsync(requestUrl,  subscriptionsToRemove);
                if(response.StatusCode is not HttpStatusCode.OK)
                    throw new HttpRequestException($"Server did not respond with {HttpStatusCode.OK} status code.");
            }
        }

        public async Task<IsUserExistDTO> CheckIfUserExists(string username)
        {
            var endpointUrl = _configuration["AuthAutority:Endpoints:CheckIfUserExist"]?.Replace("{username}", username);
            
            var requestUrl = $"{_configuration["AuthAutority:Address"]}{endpointUrl}";

            using (HttpClient client = new())
            {
                var response = await client.GetFromJsonAsync<IsUserExistDTO>(requestUrl);

                if (response is null)
                    throw new HttpRequestException($"Server respond with unexpected JSON value.");

                return response;
            }
        }
    }
}
