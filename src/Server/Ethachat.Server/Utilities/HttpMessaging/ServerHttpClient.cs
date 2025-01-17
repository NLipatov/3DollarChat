﻿using System.Net;
using System.Text;
using System.Text.Json;
using EthachatShared.Models.Authentication.Enums;
using EthachatShared.Models.Authentication.Models;
using EthachatShared.Models.Authentication.Models.Credentials.CredentialsDTO;
using EthachatShared.Models.Authentication.Models.Credentials.Implementation;
using EthachatShared.Models.Authentication.Models.UserAuthentication;
using EthachatShared.Models.Users;
using EthachatShared.Models.WebPushNotification;

namespace Ethachat.Server.Utilities.HttpMessaging
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

                return responseAuthResult ?? new AuthResult()
                {
                    Result = AuthResultType.Fail,
                };
            }
        }

        public async Task<AuthResult> Register(UserAuthentication userDTO)
        {
            using (var client = new HttpClient())
            {
                var content = new StringContent(JsonSerializer.Serialize(userDTO), Encoding.UTF8, "application/json");

                string url = _configuration["AuthAutority:Address"] + _configuration["AuthAutority:Endpoints:Register"];

                HttpResponseMessage response;
                try
                {
                    response = await client.PostAsync(url, content);
                }
                catch (Exception e)
                {
                    return new()
                    {
                        Message = "Could not get response from authentication instance.",
                        Result = AuthResultType.Fail
                    };
                }

                var serializedResponse = await response.Content.ReadAsStringAsync();
                UserAuthenticationOperationResult? deserializedResponse =
                    JsonSerializer.Deserialize<UserAuthenticationOperationResult>(serializedResponse);

                if (deserializedResponse == null)
                {
                    throw new ApplicationException("Could not deserialize the response from AuthAPI");
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

                if (response.StatusCode != HttpStatusCode.OK)
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

        public async Task<AuthResult> GetUsernameByCredentials(CredentialsDTO credentials)
        {
            var endpointUrl = _configuration["AuthAutority:Endpoints:UsernameByCredentials"];

            var requestUrl = $"{_configuration["AuthAutority:Address"]}{endpointUrl}";

            using (HttpClient client = new())
            {
                var response = await client.PostAsJsonAsync(requestUrl, credentials);
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<AuthResult>(responseContent, options: new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true
                });
                
                return result ?? new AuthResult {Message = "Could not get response from AuthAPI", Result = AuthResultType.Fail};
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