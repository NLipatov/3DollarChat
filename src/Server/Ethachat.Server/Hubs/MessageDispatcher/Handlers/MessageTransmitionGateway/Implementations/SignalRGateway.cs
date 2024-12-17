using EthachatShared.Models.Message.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Ethachat.Server.Hubs.MessageDispatcher.Handlers.MessageTransmitionGateway.Implementations;

public class SignalRGateway<T> : IMessageGateway<T> where T : IDestinationResolvable, IIdentifiable
{
    private readonly IHubContext<MessageHub> _context;

    public SignalRGateway(IHubContext<MessageHub> context)
    {
        _context = context;
    }

    public async Task TransferAsync(T data)
    {
        await _context.Clients.Group(data.Target).SendAsync("OnTransfer", data);
    }
}