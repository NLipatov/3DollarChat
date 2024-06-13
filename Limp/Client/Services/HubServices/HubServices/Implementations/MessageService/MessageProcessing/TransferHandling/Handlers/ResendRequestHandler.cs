using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.Services.InboxService;
using EthachatShared.Models.Message;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Handlers;

public class ResendRequestHandler(IMessageBox messageBox, IMessageService messageService) : ITransferHandler<ClientMessage>
{
    public async Task HandleAsync(ClientMessage clientMessage)
    {
        var message = messageBox.Messages.FirstOrDefault(x => x.Id == clientMessage.Id);
        if (message?.Type is MessageType.TextMessage)
        {
            await messageService.SendMessage(message);
        }
    }
}