using Ethachat.Client.ClientOnlyModels.Events;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Handlers;

public class OnFileTransferedHandler(ICallbackExecutor callbackExecutor) : ITransferHandler<EventMessage>
{
    public Task HandleAsync(EventMessage clientMessage)
    {
        callbackExecutor.ExecuteSubscriptionsByName(clientMessage.Id, "OnFileReceived");
        return Task.CompletedTask;
    }
}