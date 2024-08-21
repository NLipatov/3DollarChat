using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.Types;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling;

public interface ITransferProcessorResolver
{
    MessageProcessor<T> GetProcessor<T>();
    string GetEventName<T>(TransferDirection direction);
}