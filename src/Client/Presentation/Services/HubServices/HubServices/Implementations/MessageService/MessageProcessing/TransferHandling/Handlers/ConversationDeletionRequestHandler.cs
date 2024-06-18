using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.Services.InboxService;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Handlers;

public class ConversationDeletionRequestHandler(IMessageBox messageBox) : ITransferHandler<ClientMessage>
{
    public Task HandleAsync(ClientMessage clientMessage)
    {
        messageBox.Delete(clientMessage.Sender);
        return Task.CompletedTask;
    }
}