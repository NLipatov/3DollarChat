using EthachatShared.Models.Message.Interfaces;

namespace Ethachat.Server.Hubs.MessageDispatcher.Handlers.MessageTransmitionGateway;

public interface IMessageGateway<T> where T : IDestinationResolvable
{
    Task SendAsync(T data);
    Task TransferAsync(T data);
}