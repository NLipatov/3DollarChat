using Ethachat.Client.ClientOnlyModels.Events;
using Ethachat.Client.Services.InboxService;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Strategies.ReceiveStrategies;

public class ConversationDeletionRequestedStrategy(IMessageBox messageBox) : ITransferHandler<EventMessage>
{
    public Task HandleAsync(EventMessage clientMessage)
    {
        messageBox.Delete(clientMessage.Sender);
        return Task.CompletedTask;
    }
}