using System.Text;

namespace Ethachat.Server.BackendServiceProxy.AuthenticationInstance;

internal static class AuthenticationInstanceProxyService
{
    internal static void UseAuthenticationInstanceProxyService(this WebApplication app, IConfiguration configuration)
    {
        Console.WriteLine("Using AuthenticationInstance proxy.");

        app.MapPost("api/WebAuthn/assertionOptions", async context =>
        {
            using var httpClient = new HttpClient();

            var targetUrl = context.TransformToTargetUrl(configuration);

            var content = await context.GrabFormDataContent();

            var response = await httpClient.PostAsync(targetUrl, content);

            await response.ProcessResponse(context);
        });

        app.MapPost("api/WebAuthn/makeAssertion/{username}", async context =>
        {
            var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();

            using var httpClient = new HttpClient();

            var targetUrl = context.TransformToTargetUrl(configuration);

            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(targetUrl, content);

            await response.ProcessResponse(context);
        });

        app.MapPost("api/WebAuthn/makeCredentialOptions", async context =>
        {
            using var httpClient = new HttpClient();

            var targetUrl = context.TransformToTargetUrl(configuration);

            var content = await context.GrabFormDataContent();

            var response = await httpClient.PostAsync(targetUrl, content);

            await response.ProcessResponse(context);
        });

        app.MapPost("api/WebAuthn/makeCredential", async context =>
        {
            using var reader = new StreamReader(context.Request.Body);
            var jsonBody = await reader.ReadToEndAsync();

            using var httpClient = new HttpClient();

            var targetUrl = context.TransformToTargetUrl(configuration);

            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(targetUrl, content);

            await response.ProcessResponse(context);
        });
    }

    private static async Task ProcessResponse(this HttpResponseMessage responseMessage, HttpContext context)
    {
        if (!responseMessage.IsSuccessStatusCode)
        {
            var errorResponse = await responseMessage.Content.ReadAsStringAsync();
            Console.WriteLine($"Error: {responseMessage.StatusCode} - {responseMessage.ReasonPhrase}");
            await context.Response.WriteAsync(errorResponse);
        }
        else
        {
            var responseData = await responseMessage.Content.ReadAsStringAsync();
            context.Response.StatusCode = (int)responseMessage.StatusCode;
            await context.Response.WriteAsync(responseData);
        }
    }

    private static string TransformToTargetUrl(this HttpContext context, IConfiguration configuration)
    {
        var authApiUrl = configuration.GetSection("AuthAutority:Address").Value?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(authApiUrl))
            throw new ArgumentException("Auth API address not configured");

        var targetUrl = $"{authApiUrl}{context.Request.Path}";

        return targetUrl;
    }

    private static async Task<HttpContent> GrabFormDataContent(this HttpContext context)
    {
        var formData = await context.Request.ReadFormAsync();

        var requestData = new Dictionary<string, string>();
        foreach (var field in formData)
        {
#pragma warning disable CS8604 // Possible null reference argument.
            requestData.Add(field.Key, field.Value);
#pragma warning restore CS8604 // Possible null reference argument.
        }

        var content = new FormUrlEncodedContent(requestData);

        return content;
    }
}