namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling;

public interface ITransferHandlerFactory
{
    void RegisterHandler(string eventType, ITransferHandler handler);
    ITransferHandler GetMessageHandler(string eventType);
}