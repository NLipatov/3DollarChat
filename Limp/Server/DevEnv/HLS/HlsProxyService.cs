namespace Ethachat.Server.DevEnv.HLS;

internal static class HlsProxyService
{
    internal static void UseHlsProxyService(this WebApplication app, IConfiguration configuration)
    {
        Console.WriteLine("Using HLS proxy.");

        app.MapGet("/hlsapi/health", async context =>
        {
            var healthRequestUrl = string.Join('/', [configuration.GetSection("HlsApi:Address").Value, "health"]);
            Log($"Requesting {healthRequestUrl}");
            using (var client = new HttpClient())
            {
                var request = await client.GetAsync(healthRequestUrl);
                var response = await request.Content.ReadAsStringAsync();

                context.Response.StatusCode = (int) request.StatusCode;
                await context.Response.WriteAsync(response);
            }
        });
        app.MapGet("/hlsapi/get", async context =>
        {
            var query = context.Request.QueryString.ToString();

            using (var httpClient = new HttpClient())
            {
                var hlsApiUrl = configuration.GetSection("HlsApi:Address").Value;
                Console.WriteLine("Using HLS API: " + hlsApiUrl);
                var targetUrl = $"{hlsApiUrl}/get" + query;
                Console.WriteLine("Targetting url GET: " + targetUrl);

                var response = await httpClient.GetAsync(targetUrl);

                if (response.IsSuccessStatusCode)
                {
                    await response.Content.CopyToAsync(context.Response.Body);
                }
                else
                {
                    context.Response.StatusCode = (int)response.StatusCode;
                    await context.Response.WriteAsync(response.ReasonPhrase);
                }
            }
        });
        app.MapPost("/hlsapi/store", async context =>
        {
            var formData = new MultipartFormDataContent();

            var form = await context.Request.ReadFormAsync();

            foreach (var file in form.Files)
            {
                var fileContent = new StreamContent(file.OpenReadStream());
                formData.Add(fileContent, "payload", file.FileName);
            }

            using (var httpClient = new HttpClient())
            {
                var hlsApiUrl = configuration.GetSection("HlsApi:Address").Value;
                Log("Using HLS API: " + hlsApiUrl);
                var targetUrl = $"{hlsApiUrl}/store";
                Log("Targeting url POST: " + targetUrl);

                var response = await httpClient.PostAsync(targetUrl, formData);

                if (!response.IsSuccessStatusCode)
                    Log($"Error: {response.StatusCode} - {response.ReasonPhrase}");
            }
        });
    }

    private static void Log(string message)
    {
        Console.WriteLine($"{nameof(HlsProxyService)}: {message}");
    }
}