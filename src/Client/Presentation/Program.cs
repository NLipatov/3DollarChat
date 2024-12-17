using Blazored.Toast;
using Client.Application.Cryptography;
using Client.Application.Cryptography.KeyStorage;
using Client.Application.Runtime;
using Client.Infrastructure.Cryptography;
using Client.Infrastructure.Cryptography.KeyStorage;
using Client.Infrastructure.Runtime.PlatformRuntime;
using Ethachat.Client;
using Ethachat.Client.Services.Authentication;
using Ethachat.Client.Services.Authentication.Boundaries;
using Ethachat.Client.Services.Authentication.Handlers;
using Ethachat.Client.Services.Authentication.Handlers.Implementations.Jwt;
using Ethachat.Client.Services.Authentication.Handlers.Implementations.WebAuthn;
using Ethachat.Client.Services.ConcurrentCollectionManager;
using Ethachat.Client.Services.ConcurrentCollectionManager.Implementations;
using Ethachat.Client.Services.ContactsProvider;
using Ethachat.Client.Services.ContactsProvider.Implementations;
using Ethachat.Client.Services.DriveService;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor.Implementation;
using Ethachat.Client.Services.HubServices.CommonServices.SubscriptionService;
using Ethachat.Client.Services.HubServices.CommonServices.SubscriptionService.Implementation;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.AuthService;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.AuthService.Implementation;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.ExceptionLoggingService;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.ExceptionLoggingService.Implementation;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.BinaryReceiving;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.BinarySending;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.UsersService;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.UsersService.Implementation;
using Ethachat.Client.Services.InboxService;
using Ethachat.Client.Services.InboxService.Implementation;
using Ethachat.Client.Services.LocalStorageService;
using Ethachat.Client.Services.NotificationService;
using Ethachat.Client.Services.NotificationService.Implementation;
using Ethachat.Client.Services.UserAgent;
using Ethachat.Client.Services.UserAgent.Implementation;
using Ethachat.Client.Services.VersioningService;
using Ethachat.Client.Services.VideoStreamingService;
using Ethachat.Client.UI.AccountManagement.LogicHandlers;
using Ethachat.Client.UI.Chat.UI.Childs.MessageInput.Logic.MessageSenders.BrowserFile;
using Ethachat.Client.UI.Chat.UI.Childs.MessageInput.Logic.MessageSenders.Text;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SharedServices;
using ISerializerService = SharedServices.ISerializerService;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddTransient<IPlatformRuntime, JsPlatformRuntime>();
builder.Services.AddTransient<IKeyStorage, KeyStorage>();
builder.Services.AddSingleton<ICryptographyService, CryptographyService>();
builder.Services.AddSingleton<IMessageBox, MessageBox>();
builder.Services.AddTransient<IContactsProvider, ContactsProvider>();
builder.Services.AddTransient<IConcurrentCollectionManager, ConcurrentCollectionManager>();
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddSingleton<IUsersService, UsersService>();
builder.Services.AddSingleton<IMessageService, MessageService>();
builder.Services.AddSingleton<IHubServiceSubscriptionManager, HubServiceSubscriptionManager>();
builder.Services.AddTransient<ICallbackExecutor, CallbackExecutor>();
builder.Services.AddTransient<ILoginHandler, LoginHandler>();
builder.Services.AddTransient<IWebPushService, WebPushService>();
builder.Services.AddTransient<ILocalStorageService, LocalStorageService>();
builder.Services.AddTransient<IUserAgentService, UserAgentService>();
builder.Services.AddTransient<IJwtHandler, JwtAuthenticationHandler>();
builder.Services.AddTransient<IWebAuthnHandler, WebAuthnAuthenticationHandler>();
builder.Services.AddTransient<IAuthenticationHandler, AuthenticationManager>();
builder.Services.AddTransient<ILoggingService, LoggingService>();
builder.Services.AddSingleton<IBinaryReceivingManager, BinaryReceivingManager>();
builder.Services.AddTransient<IHlsStreamingService, HlsStreamingService>();
builder.Services.AddTransient<IVersionService, VersionService>();
builder.Services.AddTransient<IPlatformRuntime, JsPlatformRuntime>();
builder.Services.AddSingleton<IAuthenticationManagerBoundary, AuthenticationManagerBoundary>();
builder.Services.AddTransient<IBinarySendingManager, BinarySendingManager>();
builder.Services.AddScoped<IBrowserFileSender, BrowserFileSender>();
builder.Services.AddScoped<ITextMessageSender, TextMessageSender>();
builder.Services.AddTransient<IDriveService, DriveService>();
builder.Services.AddTransient<ISerializerService, SerializerService>();
builder.Services.AddBlazoredToast();

await builder.Build().RunAsync();