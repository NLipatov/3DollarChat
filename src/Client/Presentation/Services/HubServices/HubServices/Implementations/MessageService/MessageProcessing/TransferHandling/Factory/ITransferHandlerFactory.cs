using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Handlers;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Factory;

public interface ITransferHandlerFactory<T>
{
    void RegisterHandler(string eventType, IStrategyHandler<T> handler);
    IStrategyHandler<T> GetMessageHandler(string eventType);
}