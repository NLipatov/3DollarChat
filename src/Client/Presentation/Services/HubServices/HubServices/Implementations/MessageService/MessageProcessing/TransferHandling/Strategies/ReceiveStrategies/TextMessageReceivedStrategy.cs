using Ethachat.Client.ClientOnlyModels.Events;
using Ethachat.Client.Services.AuthenticationService.Handlers;
using Ethachat.Client.Services.InboxService;
using EthachatShared.Models.Message;
using EthachatShared.Models.Message.ClientToClientTransferData;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Strategies.ReceiveStrategies;

public class TextMessageReceivedStrategy(
    IMessageBox messageBox,
    IAuthenticationHandler authenticationHandler,
    IMessageService messageService)
    : ITransferHandler<TextMessage>
{
    public async Task HandleAsync(TextMessage eventMessage)
    {
        if (eventMessage.Total == 1 &&
            messageBox.Contains(new Message { Id = eventMessage.Id })) //not a composite message, duplicate
            return;

        await SendReceivedConfirmation(eventMessage.Id, eventMessage.Sender);

        messageBox.AddMessage(eventMessage);
    }

    private async Task SendReceivedConfirmation(Guid messageId, string messageSender)
    {
        var myUsername = await authenticationHandler.GetUsernameAsync();

        if (messageSender == myUsername)
            return;

        await messageService.SendMessage(new EventMessage
        {
            Id = messageId,
            Sender = myUsername,
            Target = messageSender,
            Type = EventType.MessageReceived
        });
    }
}