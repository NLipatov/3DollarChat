using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing;

public class MessageProcessor<T>(ITransferHandlerFactory<T> transferHandlerFactory)
{
    public async Task ProcessTransferAsync(string eventType, T decryptedData)
    {
        var handler = transferHandlerFactory.GetMessageHandler(eventType);
        await handler.HandleAsync(decryptedData);
    }
}