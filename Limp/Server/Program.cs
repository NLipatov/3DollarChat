using Ethachat.Server.Extensions;
using Ethachat.Server.Hubs;
using Ethachat.Server.Hubs.MessageDispatcher;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.MessageMarker;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.LongTermMessageStorage;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.LongTermMessageStorage.
    InMemoryStorage;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.LongTermMessageStorage.Redis
    .RedisConnectionConfigurer;
using Ethachat.Server.Hubs.UsersConnectedManaging.EventHandling;
using Ethachat.Server.Hubs.UsersConnectedManaging.EventHandling.Handlers;
using Ethachat.Server.Hubs.UsersConnectedManaging.EventHandling.OnlineUsersRequestEvent;
using Ethachat.Server.Services.LogService;
using Ethachat.Server.Services.LogService.Implementations.Seq;
using Ethachat.Server.Utilities.UsernameResolver;
using Ethachat.Server.WebPushNotifications;
using EthachatShared.Constants;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddSignalR()
    .AddMessagePackProtocol();

builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});

builder.Services.UseServerHttpClient();

builder.Services.UseKafkaService();

builder.Services.AddScoped<IUserConnectedHandler<UsersHub>, UConnectionHandler>();
builder.Services.AddScoped<IUserConnectedHandler<MessageHub>, MDConnectionHandler>();
builder.Services.AddTransient<IOnlineUsersManager, OnlineUsersManager>();
builder.Services.AddTransient<IMessageMarker, MessageMarker>();
builder.Services.AddTransient<IWebPushSender, FirebasePushSender>();
builder.Services.AddTransient<IUsernameResolverService, UsernameResolverService>();
builder.Services.AddTransient<ILogService, SeqLogService>();
builder.Services.AddTransient<IRedisConnectionConfigurer, RedisConnectionConfigurer>();
builder.Services.AddSingleton<ILongTermMessageStorageService, InMemoryLongTermStorage>();

var app = builder.Build();

app.UseResponseCompression();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();


app.MapRazorPages();
app.MapControllers();
app.MapHub<AuthHub>(HubRelativeAddresses.AuthHubRelativeAddress);
app.MapHub<UsersHub>(HubRelativeAddresses.UsersHubRelativeAddress);
app.MapHub<MessageHub>(HubRelativeAddresses.MessageHubRelativeAddress);
app.MapHub<LoggingHub>(HubRelativeAddresses.ExceptionLoggingHubRelativeAddress);
app.MapFallbackToFile("index.html");
app.MapGet("/hlsapi/get", async context =>
{
    var query = context.Request.QueryString.ToString();

    using (var httpClient = new HttpClient())
    {
        var hlsApiUrl = builder.Configuration.GetSection("HlsApi:Address").Value;
        Console.WriteLine("Using HLS API: " + hlsApiUrl);
        var targetUrl = $"http://{hlsApiUrl}/get" + query;
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
        var hlsApiUrl = builder.Configuration.GetSection("HlsApi:Address").Value;
        Console.WriteLine("Using HLS API: " + hlsApiUrl);
        var targetUrl = $"http://{hlsApiUrl}/store";
        Console.WriteLine("Targetting url POST: " + targetUrl);

        var response = await httpClient.PostAsync(targetUrl, formData);

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("Files uploaded successfully.");
        }
        else
        {
            Console.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");
        }
    }
});


app.Run();