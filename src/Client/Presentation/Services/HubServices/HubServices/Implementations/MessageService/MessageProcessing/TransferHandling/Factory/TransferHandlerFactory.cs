using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Handlers;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.
    Factory;

public class TransferHandlerFactory<T> : ITransferHandlerFactory<T>
{
    private readonly Dictionary<string, IStrategyHandler<T>> _handlers = [];

    public void RegisterHandler(string eventType, IStrategyHandler<T> handler)
    {
        _handlers.Add(eventType, handler);
    }

    public IStrategyHandler<T> GetMessageHandler(string eventType)
    {
        _handlers.TryGetValue(eventType, out var handler);

        if (handler is null)
            throw new ArgumentException($"No handler found for message type {eventType}");

        return handler;
    }
}