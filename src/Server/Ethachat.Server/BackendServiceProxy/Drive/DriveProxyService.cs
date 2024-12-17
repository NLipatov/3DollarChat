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

            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromMilliseconds(1500);
                var request = await client.GetAsync(healthRequestUrl);
                var response = await request.Content.ReadAsStringAsync();

                context.Response.StatusCode = (int) request.StatusCode;
                await context.Response.WriteAsync(response);
            }
            catch (Exception e)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync(string.Empty);
            }
        });
        
        app.MapGet("/driveapi/get", async context =>
        {
            var fileId = context.Request.Query["id"];
            if (!Guid.TryParse(fileId, out _))
                Log($"Invalid file id: \"{fileId}\" - not a guid.");
            
            var serviceUrl = GetServiceUrl(configuration);
            if (string.IsNullOrWhiteSpace(serviceUrl))
                Log($"Configuration value for {UrlKey} is invalid.");
            
            var requestUrl = string.Join('/', serviceUrl, "get?id=") + fileId;

            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromMilliseconds(1500);
                var request = await client.GetAsync(requestUrl);

                context.Response.StatusCode = (int)request.StatusCode;

                if (request.Content.Headers.ContentType != null)
                {
                    context.Response.ContentType = request.Content.Headers.ContentType.ToString();
                }

                var stream = await request.Content.ReadAsStreamAsync();
                await stream.CopyToAsync(context.Response.Body);
            }
            catch (Exception e)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("Internal server error.");
            }
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