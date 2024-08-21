namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Handlers;

public interface IStrategyHandler<T>
{
    Task HandleAsync(T message);
}