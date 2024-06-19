namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling;

public interface ITransferHandler<T>
{
    Task HandleAsync(T clientMessage);
}