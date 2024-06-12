using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing;

public class MessageProcessor(ITransferHandlerFactory transferHandlerFactory)
{
    public async Task ProcessTransferAsync(string eventType, object decryptedData)
    {
        var handler = transferHandlerFactory.GetMessageHandler(eventType);
        await handler.HandleAsync(decryptedData);
    }
}