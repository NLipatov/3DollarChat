using Limp.Client;
using Limp.Client.Cryptography;
using Limp.Client.HubInteraction.EventExecution;
using Limp.Client.HubInteraction.Handlers.MessageDecryption;
using Limp.Client.HubInteraction.Handlers.MessageDispatcherHub.AESOfferHandling;
using Limp.Client.HubInteraction.HubObservers;
using Limp.Client.HubInteraction.HubObservers.Implementations.AuthHub;
using Limp.Client.HubInteraction.HubObservers.Implementations.AuthHub.EventTypes;
using Limp.Client.HubInteraction.HubObservers.Implementations.UsersHubObserver;
using Limp.Client.HubInteraction.HubObservers.Implementations.UsersHubObserver.EventTypes;
using Limp.Client.TopicStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<ICryptographyService, CryptographyService>();
builder.Services.AddSingleton<IMessageBox, MessageBox>();
builder.Services.AddTransient<IMessageDecryptor, MessageDecryptor>();
builder.Services.AddTransient<IAESOfferHandler, AESOfferHandler>();
builder.Services.AddTransient<IEventCallbackExecutor, EventCallbackExecutor>();
builder.Services.AddSingleton<IHubObserver<UsersHubEvent>, UsersHubObserver>();
builder.Services.AddSingleton<IHubObserver<AuthHubEvent>, AuthHubObserver>();

await builder.Build().RunAsync();
