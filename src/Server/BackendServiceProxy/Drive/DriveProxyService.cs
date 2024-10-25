namespace Ethachat.Server.BackendServiceProxy.Drive;

public static class DriveProxyService
{
    private const string UrlKey = "DriveAPI:Address";
    
    internal static void UseDriveProxyService(this WebApplication app, IConfiguration configuration)
    {
        Console.WriteLine($"Using {nameof(DriveProxyService)}.");
        
        app.MapGet("/driveapi/health", async context =>
        {
            var serviceUrl = GetServiceUrl(configuration);
            if (string.IsNullOrWhiteSpace(serviceUrl))
                Log($"Configuration value for {UrlKey} is invalid.");
            
            var healthRequestUrl = string.Join('/', serviceUrl, "health");
            
            using var client = new HttpClient();
            var request = await client.GetAsync(healthRequestUrl);
            var response = await request.Content.ReadAsStringAsync();

            context.Response.StatusCode = (int) request.StatusCode;
            await context.Response.WriteAsync(response);
        });

        app.MapPost("/driveapi/save", async context =>
        {
            var formData = new MultipartFormDataContent();

            var form = await context.Request.ReadFormAsync();
            
            foreach (var file in form.Files)
            {
                var fileContent = new StreamContent(file.OpenReadStream());
                formData.Add(fileContent, "payload", file.FileName);
            }

            using var httpClient = new HttpClient();
            var serviceUrl = GetServiceUrl(configuration);
            if (string.IsNullOrWhiteSpace(serviceUrl))
                Log($"Configuration value for {UrlKey} is invalid.");
                
            var requestUrl = string.Join('/', serviceUrl, "save");
                
            var request = await httpClient.PostAsync(requestUrl, formData);
                
            if (!request.IsSuccessStatusCode)
                Log($"Error: {request.StatusCode} - {request.ReasonPhrase}");
            else
            {
                var response = await request.Content.ReadAsStringAsync();
                context.Response.StatusCode = (int) request.StatusCode;
                await context.Response.WriteAsync(response);
            }
        });
    }
    
    private static string? GetServiceUrl(IConfiguration configuration) => configuration.GetSection(UrlKey).Value;

    private static void Log(string message)
    {
        Console.WriteLine($"{nameof(DriveProxyService)}: {message}");
    }
}