using Ethachat.Client.ClientOnlyModels.Events;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Handlers;

public class TypingEventHandler(ICallbackExecutor callbackExecutor) : ITransferHandler<EventMessage>
{
    public Task HandleAsync(EventMessage clientMessage)
    {
        callbackExecutor.ExecuteSubscriptionsByName(clientMessage.Sender, "OnTyping");
        return Task.CompletedTask;
    }
}