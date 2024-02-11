using EthachatShared.Models.Message;

namespace Ethachat.Server.Hubs.MessageDispatcher.Handlers.MessageTransmitionGateway;

public interface IMessageGateway
{
    Task SendAsync(Message message);
}