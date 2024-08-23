using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Factory;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing;

public class MessageProcessor<T>(ITransferHandlerFactory<T> transferHandlerFactory) : IMessageProcessor
{
    public async Task ProcessTransferAsync(string eventType, T decryptedData)
    {
        var handler = transferHandlerFactory.GetMessageHandler(eventType) ??
                      throw new ArgumentException($"No handler registered for {eventType}");
        
        await handler.HandleAsync(decryptedData);
    }
}