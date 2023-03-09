using ClientServerCommon.Models;
using Limp.Client.Cryptography;
using Limp.Client.Cryptography.KeyStorage;
using Limp.Client.HubInteraction.EventSubscriptionManager;
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
    private HubConnection? usersHub;
    public UsersHandler
    (NavigationManager navigationManager,
    IJSRuntime jSRuntime,
    ICryptographyService cryptographyService)
    {
        _navigationManager = navigationManager;
        _jSRuntime = jSRuntime;
        _cryptographyService = cryptographyService;
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

        usersHub.On<string>("onNameResolve", async username =>
        {
            UsersHubSubscriptionManager.CallUsernameResolved(username); 
            await GuaranteeThatRSAPairWasGenerated();

            await usersHub.SendAsync("PostAnRSAPublic", username, InMemoryKeyStorage.MyRSAPublic.Value);
        });

        await usersHub.StartAsync();

        await usersHub.SendAsync("SetUsername", await JWTHelper.GetAccessToken(_jSRuntime));

        return usersHub;
    }

    public async Task GuaranteeThatRSAPairWasGenerated()
    {
        Key? myRSAPublic = InMemoryKeyStorage.MyRSAPublic;
        Key? myRSAPrivate = InMemoryKeyStorage.MyRSAPrivate;

        if((myRSAPublic != null && myRSAPrivate != null) == false)
        {
            await _cryptographyService.GenerateRSAKeyPairAsync();
        }
    }

    public void Dispose()
    {
        UsersHubSubscriptionManager.UnsubscriveAll();
    }
}
