using ClientServerCommon.Models;
using Limp.Client.Cryptography.KeyStorage;
using Limp.Client.HubInteraction.Handlers.Helpers;
using Limp.Client.HubInteraction.HubObservers;
using Limp.Client.HubInteraction.HubObservers.Implementations.UsersHubObserver.EventTypes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace Limp.Client.HubConnectionManagement.ConnectionHandlers.HubInteraction.Implementations;

public class UsersHubInteractor : IHubInteractor<UsersHubInteractor>
{
    private readonly NavigationManager _navigationManager;
    private readonly IJSRuntime _jSRuntime;
    private readonly IHubObserver<UsersHubEvent> _usersHubObserver;
    private HubConnection? usersHub;

    public UsersHubInteractor
    (NavigationManager navigationManager,
    IJSRuntime jSRuntime,
    IHubObserver<UsersHubEvent> usersHubSubscriptionManager)
    {
        _navigationManager = navigationManager;
        _jSRuntime = jSRuntime;
        _usersHubObserver = usersHubSubscriptionManager;
    }

    public async Task<HubConnection> ConnectAsync()
    {
        usersHub = new HubConnectionBuilder()
        .WithUrl(_navigationManager.ToAbsoluteUri("/usersHub"))
        .Build();

        #region Event handlers registration
        //Here we are registering a callbacks for specific server-triggered events.
        //Events are being triggered from SignalR hubs in server project.

        usersHub.On<List<UserConnection>>("ReceiveOnlineUsers", async updatedTrackedUserConnections =>
        {
            await _usersHubObserver.CallHandler(UsersHubEvent.ConnectedUsersListReceived, updatedTrackedUserConnections);
        });

        usersHub.On<string>("ReceiveConnectionId", async connectionId =>
        {
            await _usersHubObserver.CallHandler(UsersHubEvent.ConnectionIdReceived, connectionId);
        });

        usersHub.On<string>("onNameResolve", async username =>
        {
            await _usersHubObserver.CallHandler(UsersHubEvent.MyUsernameResolved, username);

            await usersHub.SendAsync("PostAnRSAPublic", username, InMemoryKeyStorage.MyRSAPublic.Value);
        });
        #endregion

        await usersHub.StartAsync();

        await usersHub.SendAsync("SetUsername", await JWTHelper.GetAccessToken(_jSRuntime));

        return usersHub;
    }

    public async ValueTask DisposeAsync()
    {
        _usersHubObserver.UnsubscriveAll();
        if (usersHub != null)
        {
            await usersHub.StopAsync();
            await usersHub.DisposeAsync();
        }
    }
}
