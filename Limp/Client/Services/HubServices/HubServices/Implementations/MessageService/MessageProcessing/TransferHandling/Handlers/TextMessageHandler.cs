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
    : ITransferHandler<TextMessage>
{
    public async Task HandleAsync(TextMessage clientMessage)
    {
        if (clientMessage.Total == 1 &&
            messageBox.Contains(new Message { Id = clientMessage.Id })) //not a composite message, duplicate
            return;

        await SendReceivedConfirmation(clientMessage.Id, clientMessage.Sender);

        messageBox.AddMessage(clientMessage);
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