using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Handlers;

public class OnFileTransferedHandler(ICallbackExecutor callbackExecutor) : ITransferHandler<ClientMessage>
{
    public Task HandleAsync(ClientMessage clientMessage)
    {
        callbackExecutor.ExecuteSubscriptionsByName(clientMessage.Id, "OnFileReceived");
        return Task.CompletedTask;
    }
}