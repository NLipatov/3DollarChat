using EthachatShared.Models.Message;
using Microsoft.AspNetCore.SignalR;

namespace Ethachat.Server.Hubs.MessageDispatcher.Handlers.MessageTransmitionGateway.Implementations;

public class SignalRGateway : IMessageGateway
{
    private readonly IHubContext<MessageHub> _context;

    public SignalRGateway(IHubContext<MessageHub> context)
    {
        _context = context;
    }
    public async Task SendAsync(Message message)
    {
        await _context.Clients.Group(message.TargetGroup!).SendAsync("ReceiveMessage", message);
    }
}