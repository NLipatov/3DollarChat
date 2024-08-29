using Ethachat.Server.Hubs.MessageDispatcher.Handlers.MessageTransmitionGateway.Implementations;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.LongTermMessageStorage;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.SenderImplementations.
    EncryptedData;
using Ethachat.Server.Hubs.UsersConnectedManaging.ConnectedUserStorage;
using Ethachat.Server.Hubs.UsersConnectedManaging.EventHandling;
using Ethachat.Server.Hubs.UsersConnectedManaging.EventHandling.OnlineUsersRequestEvent;
using Ethachat.Server.Services.Notifications.WebPush;
using Ethachat.Server.Utilities.UsernameResolver;
using EthachatShared.Contracts;
using EthachatShared.Models.Authentication.Models;
using EthachatShared.Models.Authentication.Models.Credentials.CredentialsDTO;
using EthachatShared.Models.ConnectedUsersManaging;
using EthachatShared.Models.Message;
using EthachatShared.Models.Message.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Ethachat.Server.Hubs.MessageDispatcher;

public class MessageHub : Hub
{
    private readonly IUserConnectedHandler<MessageHub> _userConnectedHandler;
    private readonly IOnlineUsersManager _onlineUsersManager;
    private readonly IWebPushNotificationService _webPushNotificationService;
    private readonly ILongTermStorageService<ClientToClientData> _longTermTransferStorageService;
    private readonly IUsernameResolverService _usernameResolverService;
    private static IReliableSender<ClientToClientData> _reliableTransferDataSender;
    private static IHubContext<MessageHub> _context;

    public MessageHub
    (IUserConnectedHandler<MessageHub> userConnectedHandler,
        IOnlineUsersManager onlineUsersManager,
        IWebPushNotificationService webPushNotificationService,
        ILongTermStorageService<ClientToClientData> longTermTransferStorageService,
        IUsernameResolverService usernameResolverService,
        IHubContext<MessageHub> context)
    {
        _context = context;
        _userConnectedHandler = userConnectedHandler;
        _onlineUsersManager = onlineUsersManager;
        _webPushNotificationService = webPushNotificationService;
        _longTermTransferStorageService = longTermTransferStorageService;
        _usernameResolverService = usernameResolverService;

        if (_reliableTransferDataSender is null)
        {
            _reliableTransferDataSender =
                new ClientToClientDataReliableSender(new SignalRGateway<ClientToClientData>(_context),
                    _longTermTransferStorageService);
        }
    }

    public override async Task OnConnectedAsync()
    {
        InMemoryHubConnectionStorage.MessageDispatcherHubConnections.TryAdd(Context.ConnectionId,
            new List<string> { Context.ConnectionId });

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var keys = InMemoryHubConnectionStorage.MessageDispatcherHubConnections
            .Where(x => x.Value.Contains(Context.ConnectionId)).Select(x => x.Key);

        foreach (var key in keys)
        {
            var oldConnections = InMemoryHubConnectionStorage.MessageDispatcherHubConnections[key];
            var newConnections = oldConnections.Where(x => x != Context.ConnectionId).ToList();
            if (newConnections.Any())
                InMemoryHubConnectionStorage.MessageDispatcherHubConnections.TryUpdate(key, newConnections,
                    oldConnections);
            else
                InMemoryHubConnectionStorage.MessageDispatcherHubConnections.TryRemove(key, out _);

            await PushOnlineUsersToClients();
        }

        await base.OnDisconnectedAsync(exception);
    }

    private static bool IsClientConnectedToHub(string username)
    {
        lock (InMemoryHubConnectionStorage.MessageDispatcherHubConnections)
        {
            return InMemoryHubConnectionStorage.MessageDispatcherHubConnections.Any(x => x.Key == username);
        }
    }

    public async Task SetUsername(CredentialsDTO credentialsDto)
    {
        AuthResult usernameRequestResult = await _usernameResolverService.GetUsernameAsync(credentialsDto);
        if (usernameRequestResult.Result is not AuthResultType.Success)
        {
            await Clients.Caller.SendAsync("OnAccessTokenInvalid", usernameRequestResult);
        }

        var usernameFromToken = usernameRequestResult.Message ?? string.Empty;

        await _userConnectedHandler.OnUsernameResolved
        (Context.ConnectionId,
            usernameFromToken,
            Groups.AddToGroupAsync,
            Clients.Caller.SendAsync,
            Clients.Caller.SendAsync,
            webAuthnPair: credentialsDto.WebAuthnPair,
            jwtPair: credentialsDto.JwtPair);

        var keys = InMemoryHubConnectionStorage.MessageDispatcherHubConnections
            .Where(x => x.Value.Contains(Context.ConnectionId)).Select(x => x.Key);

        foreach (var key in keys)
        {
            var connections = InMemoryHubConnectionStorage.MessageDispatcherHubConnections[key];
            InMemoryHubConnectionStorage.MessageDispatcherHubConnections.TryRemove(key, out _);
            InMemoryHubConnectionStorage.MessageDispatcherHubConnections.TryAdd(usernameFromToken, connections);

            await PushOnlineUsersToClients();
        }

        var storedTransferData = await _longTermTransferStorageService.GetSaved(usernameFromToken);
        foreach (var data in storedTransferData)
        {
            await TransferAsync(data);
        }

        await Clients.Caller.SendAsync("Authenticated", Context.ConnectionId);
    }

    public async Task PushOnlineUsersToClients()
    {
        UserConnectionsReport report = _onlineUsersManager.FormUsersOnlineMessage();
        await Clients.All.SendAsync("ReceiveOnlineUsers", report);
    }

    public async Task TransferAsync(ClientToClientData dataClientToClientData)
    {
        if (IsClientConnectedToHub(dataClientToClientData.Target))
            await _reliableTransferDataSender.EnqueueAsync(dataClientToClientData);
        else
        {
            _longTermTransferStorageService.SaveAsync(dataClientToClientData);
            SendNotificationAsync(dataClientToClientData);
        }

        await _context.Clients.Group(dataClientToClientData.Target).SendAsync("OnTransfer", dataClientToClientData);
    }

    private async Task SendNotificationAsync<T>(T itemToNotifyAbout)
        where T : IHasInnerDataType, ISourceResolvable, IDestinationResolvable, IWebPushNotice
    {
        var isReceiverOnline = IsClientConnectedToHub(itemToNotifyAbout.Target);
        if (!isReceiverOnline)
        {
            await _webPushNotificationService.SendAsync(itemToNotifyAbout);
        }
    }

    public void OnTransferAcked(Guid id)
    {
        _reliableTransferDataSender.OnAck(id);
    }
}