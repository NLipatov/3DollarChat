using ClientServerCommon.Models;
using Limp.Client.Cryptography;
using Limp.Client.Cryptography.KeyStorage;
using Limp.Client.HubInteraction.EventSubscriptionManager.UsersHub.Contract;
using Limp.Client.HubInteraction.EventSubscriptionManager.UsersHub.EventTypes;
using Limp.Client.HubInteraction.Handlers.Helpers;
using LimpShared.Encryption;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace Limp.Client.HubInteraction.Handlers;

public class UsersHandler : IHandler<UsersHandler>
{
    private readonly NavigationManager _navigationManager;
    private readonly IJSRuntime _jSRuntime;
    private readonly ICryptographyService _cryptographyService;
    private readonly IUsersHubSubscriptionManager _usersHubSubscriptionManager;
    private HubConnection? usersHub;
    public UsersHandler
    (NavigationManager navigationManager,
    IJSRuntime jSRuntime,
    ICryptographyService cryptographyService,
    IUsersHubSubscriptionManager usersHubSubscriptionManager)
    {
        _navigationManager = navigationManager;
        _jSRuntime = jSRuntime;
        _cryptographyService = cryptographyService;
        _usersHubSubscriptionManager = usersHubSubscriptionManager;
    }

    public async Task<HubConnection> ConnectAsync()
    {
        usersHub = new HubConnectionBuilder()
        .WithUrl(_navigationManager.ToAbsoluteUri("/usersHub"))
        .Build();

        usersHub.On<List<UserConnections>>("ReceiveOnlineUsers", async updatedTrackedUserConnections =>
        {
            await _usersHubSubscriptionManager.CallHandler(UserHubEventType.ConnectedUsersListReceived, updatedTrackedUserConnections);
        });

        usersHub.On<string>("ReceiveConnectionId", async connectionId =>
        {
            await _usersHubSubscriptionManager.CallHandler(UserHubEventType.ConnectionIdReceived ,connectionId);
        });

        usersHub.On<string>("onNameResolve", async username =>
        {
            await _usersHubSubscriptionManager.CallHandler(UserHubEventType.MyUsernameResolved, username);

            await usersHub.SendAsync("PostAnRSAPublic", username, InMemoryKeyStorage.MyRSAPublic.Value);
        });

        await usersHub.StartAsync();

        await usersHub.SendAsync("SetUsername", await JWTHelper.GetAccessToken(_jSRuntime));

        return usersHub;
    }

    public void Dispose()
    {
        _usersHubSubscriptionManager.UnsubscriveAll();
        DisposeUsersHub();
    }

    private async Task DisposeUsersHub()
    {
        if(usersHub != null)
        {
            await usersHub.StopAsync();
            await usersHub.DisposeAsync();
        }
    }
}
