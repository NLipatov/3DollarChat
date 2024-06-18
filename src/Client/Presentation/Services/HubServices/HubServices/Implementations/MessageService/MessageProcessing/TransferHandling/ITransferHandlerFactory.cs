namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling;

public interface ITransferHandlerFactory<T>
{
    void RegisterHandler(string eventType, ITransferHandler<T> handler);
    ITransferHandler<T> GetMessageHandler(string eventType);
}