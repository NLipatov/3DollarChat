using System.Text;

namespace Ethachat.Server.BackendServiceProxy.AuthAPI;

internal static class AuthApiProxyService
{
    internal static void UseAuthApiProxyService(this WebApplication app, IConfiguration configuration)
    {
        Log("Auth API Proxy will be used.");

        app.MapPost("/api/WebAuthn/assertionOptions", async context =>
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.PostAsync(GetAuthApiUrl(configuration, context),
                await GetFormDataContentAsync(context));

            if (!response.IsSuccessStatusCode)
                Log($"Error: {response.StatusCode} - {response.ReasonPhrase}");
            else
            {
                var responseData = await response.Content.ReadAsStringAsync();
                var responseDataBytes = Encoding.UTF8.GetBytes(responseData);
                await context.Response.Body.WriteAsync(responseDataBytes, 0, responseDataBytes.Length);
            }
        });

        app.MapPost("/api/WebAuthn/makeAssertion/{username}", async context =>
        {
            var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();

            using (var httpClient = new HttpClient())
            {
                var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(GetAuthApiUrl(configuration, context), content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                    await context.Response.WriteAsync(errorResponse);
                }
                else
                {
                    var responseData = await response.Content.ReadAsStringAsync();
                    context.Response.StatusCode = (int)response.StatusCode;
                    await context.Response.WriteAsync(responseData);
                }
            }
        });

        app.MapPost("/api/WebAuthn/makeCredentialOptions", async context =>
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.PostAsync(GetAuthApiUrl(configuration, context),
                await GetFormDataContentAsync(context));

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                Log($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                await context.Response.WriteAsync(errorResponse);
            }
            else
            {
                var responseData = await response.Content.ReadAsStringAsync();
                context.Response.StatusCode = (int)response.StatusCode;
                await context.Response.WriteAsync(responseData);
            }
        });

        app.MapPost("/api/WebAuthn/makeCredential", async context =>
        {
            using var reader = new StreamReader(context.Request.Body);
            var jsonBody = await reader.ReadToEndAsync();

            using var httpClient = new HttpClient();
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(GetAuthApiUrl(configuration, context), content);

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                Log($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                await context.Response.WriteAsync(errorResponse);
            }
            else
            {
                var responseData = await response.Content.ReadAsStringAsync();
                context.Response.StatusCode = (int)response.StatusCode;
                await context.Response.WriteAsync(responseData);
            }
        });
    }

    private static string GetAuthApiUrl(IConfiguration configuration, HttpContext context)
    {
        var authApiAddress = configuration.GetSection("AuthAutority:Address").Value?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(authApiAddress))
        {
            Log("Could not get auth api address from configuration");
            throw new ArgumentException("Auth API address not configured");
        }
    
        Log($"Using auth api address: {authApiAddress}");

        if (authApiAddress.EndsWith("/"))
        {
            authApiAddress = authApiAddress.Remove(authApiAddress.Length - 1);
            Log($"Removing trailing slash. New auth api address: {authApiAddress}");
        }

        var requestPath = context.Request.Path.ToString().TrimStart('/');

        var targetUrl = $"{authApiAddress}/{requestPath}";

        Log($"target url: {targetUrl}");
    
        return targetUrl;
    }

    private static async Task<HttpContent> GetFormDataContentAsync(HttpContext context)
    {
        var formData = await context.Request.ReadFormAsync();

        var requestData = new Dictionary<string, string>();
        foreach (var field in formData)
        {
            requestData.Add(field.Key, field.Value);
        }

        return new FormUrlEncodedContent(requestData);
    }

    private static void Log(string message)
    {
        Console.WriteLine($"{nameof(AuthApiProxyService)}: {message}");
    }
}