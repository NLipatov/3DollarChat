using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.Services.AuthenticationService.Handlers;
using Ethachat.Client.Services.InboxService;
using EthachatShared.Models.Message;
using EthachatShared.Models.Message.ClientToClientTransferData;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.
    TransferHandling.Handlers;

public class TextMessageHandler(
    IMessageBox messageBox,
    IAuthenticationHandler authenticationHandler,
    IMessageService messageService)
    : ITransferHandler
{
    public async Task HandleAsync(object data)
    {
        var textMessage = (TextMessage)data;
        if (textMessage.Total == 1 &&
            messageBox.Contains(new Message { Id = textMessage.Id })) //not a composite message, duplicate
            return;

        await SendReceivedConfirmation(textMessage.Id, textMessage.Sender);

        messageBox.AddMessage(textMessage);
    }

    private async Task SendReceivedConfirmation(Guid messageId, string messageSender)
    {
        var myUsername = await authenticationHandler.GetUsernameAsync();

        if (messageSender == myUsername)
            return;

        await messageService.SendMessage(new ClientMessage
        {
            Id = messageId,
            Sender = myUsername,
            Target = messageSender,
            Type = MessageType.MessageReceivedConfirmation
        });
    }
}