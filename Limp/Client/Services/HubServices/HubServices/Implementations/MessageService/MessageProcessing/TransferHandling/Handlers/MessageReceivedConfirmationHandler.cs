using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Handlers;

public class MessageReceivedConfirmationHandler(ICallbackExecutor callbackExecutor) : ITransferHandler
{
    public Task HandleAsync(object transfer)
    {
        var clientMessage = (ClientMessage)transfer;
        callbackExecutor.ExecuteSubscriptionsByName(clientMessage.Id,
            "OnReceiverMarkedMessageAsReceived");
        return Task.CompletedTask;
    }
}