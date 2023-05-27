using Limp.Server.Extensions;
using Limp.Server.Hubs;
using Limp.Server.Hubs.MessageDispatcher;
using Limp.Server.Hubs.MessageDispatcher.Helpers.MessageSender;
using Limp.Server.Hubs.MessageDispatcher.Helpers.UndeliveredMessagesRegistry;
using Limp.Server.Hubs.UsersConnectedManaging.EventHandling;
using Limp.Server.Hubs.UsersConnectedManaging.EventHandling.Handlers;
using Limp.Server.Hubs.UsersConnectedManaging.EventHandling.OnlineUsersRequestEvent;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddSignalR();
builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});

builder.Services.UseServerHttpClient();

builder.Services.UseKafkaService();

builder.Services.AddScoped<IUserConnectedHandler<UsersHub>,  UConnectionHandler>();
builder.Services.AddScoped<IUserConnectedHandler<MessageHub>, MDConnectionHandler>();
builder.Services.AddTransient<IOnlineUsersManager, OnlineUsersManager>();
builder.Services.AddTransient<IMessageSendHandler, MessageSendHandler>();
builder.Services.AddSingleton<IUndeliveredMessagesStorer, UndeliveredMessagesStorer>();

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
app.MapHub<AuthHub>("/authHub");
app.MapHub<UsersHub>("/usersHub");
app.MapHub<MessageHub>("/messageDispatcherHub");
app.MapFallbackToFile("index.html");

app.Run();
