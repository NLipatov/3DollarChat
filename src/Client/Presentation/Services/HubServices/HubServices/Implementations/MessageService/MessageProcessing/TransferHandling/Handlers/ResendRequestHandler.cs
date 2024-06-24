using Ethachat.Client.ClientOnlyModels.Events;
using Ethachat.Client.Services.InboxService;
using EthachatShared.Models.Message;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Handlers;

public class ResendRequestHandler(IMessageBox messageBox, IMessageService messageService) : ITransferHandler<EventMessage>
{
    public async Task HandleAsync(EventMessage clientMessage)
    {
        var message = messageBox.Messages.FirstOrDefault(x => x.Id == clientMessage.Id);
        if (message?.Type is MessageType.TextMessage)
        {
            await messageService.SendMessage(message);
        }
    }
}