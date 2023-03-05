using ClientServerCommon.Models;
using Limp.Client.HubInteraction.EventSubscriptionManager;
using Limp.Client.HubInteraction.Handlers.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace Limp.Client.HubInteraction.Handlers;

public class UsersHandler : IHandler<UsersHandler>
{
    private readonly NavigationManager _navigationManager;
    private readonly IJSRuntime _jSRuntime;
    private HubConnection? usersHub;
    public UsersHandler
        (NavigationManager navigationManager,
        IJSRuntime jSRuntime)
    {
        _navigationManager = navigationManager;
        _jSRuntime = jSRuntime;
    }

    public async Task<HubConnection> ConnectAsync()
    {
        usersHub = new HubConnectionBuilder()
        .WithUrl(_navigationManager.ToAbsoluteUri("/usersHub"))
        .Build();

        usersHub.On<List<UserConnections>>("ReceiveOnlineUsers", updatedTrackedUserConnections =>
        {
            UsersHubSubscriptionManager.CallUsersConnectionsReceived(updatedTrackedUserConnections);
        });

        usersHub.On<string>("ReceiveConnectionId", connectionId =>
        {
            UsersHubSubscriptionManager.CallConnectionIdReceived(connectionId);
        });

        usersHub.On<string>("onNameResolve", username =>
        {
            UsersHubSubscriptionManager.CallUsernameResolved(username);
        });

        await usersHub.StartAsync();

        await usersHub.SendAsync("SetUsername", await JWTHelper.GetAccessToken(_jSRuntime));

        return usersHub;
    }
}
