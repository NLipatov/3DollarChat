namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling;

public interface ITransferHandler
{
    Task HandleAsync(object transfer);
}