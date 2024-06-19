namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.
    TransferHandling;

public class TransferHandlerFactory<T> : ITransferHandlerFactory<T>
{
    private Dictionary<string, ITransferHandler<T>> _handlers = [];

    public void RegisterHandler(string eventType, ITransferHandler<T> handler)
    {
        _handlers.Add(eventType, handler);
    }

    public ITransferHandler<T> GetMessageHandler(string eventType)
    {
        _handlers.TryGetValue(eventType, out var handler);

        if (handler is null)
            throw new ArgumentException($"No handler found for message type {eventType}");

        return handler;
    }
}