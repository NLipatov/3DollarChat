using LimpShared.Models.Authentication.Models;
using LimpShared.Models.Authentication.Models.AuthenticatedUserRepresentation.PublicKey;
using LimpShared.Models.Authentication.Models.UserAuthentication;
using LimpShared.Models.Users;
using LimpShared.Models.WebPushNotification;
using System.Net;
using System.Text;
using System.Text.Json;
using LimpShared.Models.Authentication.Enums;
using LimpShared.Models.Authentication.Models.Credentials.CredentialsDTO;
using LimpShared.Models.Authentication.Models.Credentials.Implementation;

namespace Limp.Server.Utilities.HttpMessaging
{
    public class ServerHttpClient : IServerHttpClient
    {
        private static IConfiguration _configuration;

        public ServerHttpClient(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<AuthResult> ValidateCredentials(CredentialsDTO credentials)
        {
            using (var client = new HttpClient())
            {
                var requestUrl =
                    $"{_configuration["AuthAutority:Address"]}{_configuration["AuthAutority:Endpoints:ValidateCredentials"]}";

                var request = await client.PostAsJsonAsync(requestUrl, credentials);

                var response = await request.Content.ReadAsStringAsync();

                var result = JsonSerializer.Deserialize<AuthResult>(response, options: new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true
                });

                return result ?? new AuthResult {Result = AuthResultType.Fail};
            }
        }

        public async Task<AuthResult> RefreshCredentials(CredentialsDTO credentials)
        {
            using (var client = new HttpClient())
            {
                var content = new StringContent(JsonSerializer.Serialize(credentials), Encoding.UTF8, "application/json");

                var requestUrl =
                    $"{_configuration["AuthAutority:Address"]}{_configuration["AuthAutority:Endpoints:RefreshCredentials"]}";

                var response = await client.PostAsync(requestUrl, content);
                
                var responseContent = await response.Content.ReadAsStringAsync();

                var responseAuthResult = JsonSerializer.Deserialize<AuthResult>(responseContent, options: new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true
                });
                
                return new AuthResult
                {
                    Message = responseAuthResult?.Message,
                    Result = response.StatusCode is not HttpStatusCode.OK ? AuthResultType.Fail : AuthResultType.Success,
                    JwtPair = responseAuthResult?.JwtPair,
                    CredentialId = responseAuthResult?.CredentialId ?? string.Empty
                };
            }
        }

        public async Task<AuthResult> Register(UserAuthentication userDTO)
        {
            using (var client = new HttpClient())
            {
                var content = new StringContent(JsonSerializer.Serialize(userDTO), Encoding.UTF8, "application/json");

                string url = _configuration["AuthAutority:Address"] + _configuration["AuthAutority:Endpoints:Register"];

                var response = await client.PostAsync(url, content);

                var serializedResponse = await response.Content.ReadAsStringAsync();
                UserAuthenticationOperationResult? deserializedResponse =
                    JsonSerializer.Deserialize<UserAuthenticationOperationResult>(serializedResponse);

                if (deserializedResponse == null)
                {
                    throw new ApplicationException("Could not get response from AuthAPI");
                }

                return new AuthResult
                {
                    Message = deserializedResponse.SystemMessage,
                    Result = deserializedResponse.ResultType == OperationResultType.Success
                        ? AuthResultType.Success
                        : AuthResultType.Fail,
                };
            }
        }

        public async Task<AuthResult> GetJWTPairAsync(UserAuthentication userDTO)
        {
            using (var client = new HttpClient())
            {
                var content = new StringContent(JsonSerializer.Serialize(userDTO), Encoding.UTF8, "application/json");

                var response =
                    await client.PostAsync(
                        _configuration["AuthAutority:Address"] + _configuration["AuthAutority:Endpoints:Get-Token"],
                        content);

                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    return new AuthResult
                    {
                        Message = await response.Content.ReadAsStringAsync(),
                        Result = AuthResultType.Fail,
                    };
                }

                var responseContent = await response.Content.ReadAsStringAsync();

                var jwtPair = JsonSerializer.Deserialize<JwtPair>(responseContent);

                return new AuthResult
                {
                    Result = AuthResultType.Success,
                    JwtPair = jwtPair,
                };
            }
        }

        public async Task<TokenRelatedOperationResult> GetUserNameFromAccessTokenAsync(string accessToken)
        {
            using (var client = new HttpClient())
            {
                var requestUrl =
                    $"{_configuration["AuthAutority:Address"]}{_configuration["AuthAutority:Endpoints:GetUserName"]}?accessToken={accessToken}";

                var response = await client.GetAsync(requestUrl);

                TokenRelatedOperationResult result =
                    JsonSerializer.Deserialize<TokenRelatedOperationResult>(await response.Content.ReadAsStringAsync());

                return result;
            }
        }

        public async Task PostAnRSAPublic(PublicKeyDto publicKeyDTO)
        {
            var requestUrl =
                $"{_configuration["AuthAutority:Address"]}{_configuration["AuthAutority:Endpoints:RSAPublic"]}";

            using (HttpClient client = new())
            {
                await client.PostAsJsonAsync(requestUrl,
                    publicKeyDTO);
            }
        }

        public async Task<string?> GetAnRSAPublicKey(string username)
        {
            var requestUrl =
                $"{_configuration["AuthAutority:Address"]}{_configuration["AuthAutority:Endpoints:RSAPublic"]}/{username}";

            using (HttpClient client = new())
            {
                var response = await client.GetAsync(requestUrl);
                var pemEncodedKey = await response.Content.ReadAsStringAsync();
                return pemEncodedKey;
            }
        }

        public async Task AddUserWebPushSubscribtion(NotificationSubscriptionDto subscriptionDTO)
        {
            var requestUrl =
                $"{_configuration["AuthAutority:Address"]}{_configuration["AuthAutority:Endpoints:SubscribeToWebPush"]}";

            using (HttpClient client = new())
            {
                await client.PutAsJsonAsync(requestUrl, subscriptionDTO);
            }
        }

        public async Task<NotificationSubscriptionDto[]> GetUserWebPushSubscriptionsByAccessToken(string username)
        {
            var requestUrl =
                $"{_configuration["AuthAutority:Address"]}{_configuration["AuthAutority:Endpoints:GetNotificationsByUserId"]}/{username}";

            using (HttpClient client = new())
            {
                var response = await client.GetAsync(requestUrl);
                var serializedSubscriptions = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<NotificationSubscriptionDto[]>(serializedSubscriptions,
                    options: new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new NotificationSubscriptionDto[0];
            }
        }

        public async Task RemoveUserWebPushSubscriptions(NotificationSubscriptionDto[] subscriptionsToRemove)
        {
            var requestUrl =
                $"{_configuration["AuthAutority:Address"]}{_configuration["AuthAutority:Endpoints:RemoveWebPushSubscriptions"]}";

            using (HttpClient client = new())
            {
                var response = await client.PatchAsJsonAsync(requestUrl, subscriptionsToRemove);
                if (response.StatusCode is not HttpStatusCode.OK)
                    throw new HttpRequestException($"Server did not respond with {HttpStatusCode.OK} status code.");
            }
        }

        public async Task<List<AccessRefreshEventLog>> GetTokenRefreshHistory(string accessToken)
        {
            var endpointUrl = _configuration["AuthAutority:Endpoints:GetTokenRefreshHistory"];

            var requestUrl = $"{_configuration[$"AuthAutority:Address"]}{endpointUrl}?accessToken={accessToken}";

            using (HttpClient client = new())
            {
                var response = await client.GetFromJsonAsync<List<AccessRefreshEventLog>>(requestUrl);

                if (response is null)
                    throw new HttpRequestException($"Server respond with unexpected JSON value.");

                return response;
            }
        }

        public Task<string> GetServerAddress()
        {
            string authorityAddressKey = "AuthAutority:Address";
            return Task.FromResult(_configuration[authorityAddressKey]
                                   ??
                                   throw new ArgumentException
                                       ($"Could not get a value by key {authorityAddressKey} from server configuration."));
        }

        public async Task<string> GetUsernameByCredentialId(string credentialId)
        {
            var escapedCredentialId = Uri.EscapeDataString(credentialId);
            var endpointUrl = _configuration["AuthAutority:Endpoints:UsernameByCredentialId"]
                ?.Replace("{credentialId}", escapedCredentialId);

            var requestUrl = $"{_configuration["AuthAutority:Address"]}{endpointUrl}";

            using (HttpClient client = new())
            {
                var response = await client.GetStringAsync(requestUrl);

                if (response is null)
                    throw new HttpRequestException($"Server respond with unexpected JSON value.");

                return response;
            }
        }

        public async Task<IsUserExistDto> CheckIfUserExists(string username)
        {
            var endpointUrl = _configuration["AuthAutority:Endpoints:CheckIfUserExist"]
                ?.Replace("{username}", username);

            var requestUrl = $"{_configuration["AuthAutority:Address"]}{endpointUrl}";

            using (HttpClient client = new())
            {
                var response = await client.GetFromJsonAsync<IsUserExistDto>(requestUrl);

                if (response is null)
                    throw new HttpRequestException($"Server respond with unexpected JSON value.");

                return response;
            }
        }
    }
}