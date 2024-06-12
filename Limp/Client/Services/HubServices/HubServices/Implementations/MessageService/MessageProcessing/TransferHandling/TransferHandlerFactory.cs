namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.
    TransferHandling;

public class TransferHandlerFactory : ITransferHandlerFactory
{
    private Dictionary<string, ITransferHandler> _handlers = [];

    public void RegisterHandler(string eventType, ITransferHandler handler)
    {
        _handlers.Add(eventType, handler);
    }

    public ITransferHandler GetMessageHandler(string eventType)
    {
        _handlers.TryGetValue(eventType, out var handler);

        if (handler is null)
            throw new ArgumentException($"No textMessageHandler found for message type {eventType}");

        return handler;
    }
}