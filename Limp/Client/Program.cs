using BlazorBootstrap;
using Limp.Client;
using Limp.Client.Cryptography;
using Limp.Client.HubConnectionManagement.HubObservers.Implementations.MessageHub;
using Limp.Client.HubConnectionManagement.HubObservers.Implementations.MessageHub.EventTypes;
using Limp.Client.HubInteraction.EventExecution;
using Limp.Client.HubInteraction.Handlers.MessageDecryption;
using Limp.Client.HubInteraction.Handlers.MessageDispatcherHub.AESOfferHandling;
using Limp.Client.HubInteraction.HubObservers;
using Limp.Client.HubInteraction.HubObservers.Implementations.AuthHub;
using Limp.Client.HubInteraction.HubObservers.Implementations.AuthHub.EventTypes;
using Limp.Client.HubInteraction.HubObservers.Implementations.UsersHubObserver;
using Limp.Client.HubInteraction.HubObservers.Implementations.UsersHubObserver.EventTypes;
using Limp.Client.Services.ConcurrentCollectionManager;
using Limp.Client.Services.ConcurrentCollectionManager.Implementations;
using Limp.Client.Services.ContactsProvider;
using Limp.Client.Services.ContactsProvider.Implementations;
using Limp.Client.Services.HubConnectionProvider;
using Limp.Client.Services.HubConnectionProvider.Implementation;
using Limp.Client.Services.HubService.AuthService;
using Limp.Client.Services.HubService.AuthService.Implementation;
using Limp.Client.Services.HubService.UsersService;
using Limp.Client.Services.HubService.UsersService.Implementation;
using Limp.Client.TopicStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddBlazorBootstrap();
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<ICryptographyService, CryptographyService>();
builder.Services.AddSingleton<IMessageBox, MessageBox>();
builder.Services.AddTransient<IMessageDecryptor, MessageDecryptor>();
builder.Services.AddTransient<IAESOfferHandler, AESOfferHandler>();
builder.Services.AddTransient<IEventCallbackExecutor, EventCallbackExecutor>();
builder.Services.AddTransient<IContactsProvider, ContactsProvider>();
builder.Services.AddTransient<IConcurrentCollectionManager, ConcurrentCollectionManager>();
builder.Services.AddScoped<IHubConnectionProvider, HubConnectionProvider>();
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddSingleton<IUsersService, UsersService>();
#region HubObservers DI Registration
builder.Services.AddSingleton<IHubObserver<UsersHubEvent>, UsersHubObserver>();
builder.Services.AddSingleton<IHubObserver<AuthHubEvent>, AuthHubObserver>();
builder.Services.AddSingleton<IHubObserver<MessageHubEvent>, MessageHubObserver>();
#endregion

await builder.Build().RunAsync();
