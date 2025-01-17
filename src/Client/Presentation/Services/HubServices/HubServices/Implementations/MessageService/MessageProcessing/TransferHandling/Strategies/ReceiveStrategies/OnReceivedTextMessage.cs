using Client.Transfer.Domain.TransferedEntities.Events;
using Ethachat.Client.Services.Authentication.Handlers;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Handlers;
using Ethachat.Client.Services.InboxService;
using EthachatShared.Models.Message;
using EthachatShared.Models.Message.ClientToClientTransferData;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Strategies.ReceiveStrategies;

public class OnReceivedTextMessage(
    IMessageBox messageBox,
    IAuthenticationHandler authenticationHandler,
    IMessageService messageService)
    : IStrategyHandler<TextMessage>
{
    public async Task HandleAsync(TextMessage textMessage)
    {
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

        await messageService.TransferAsync(new EventMessage
        {
            Id = messageId,
            Sender = myUsername,
            Target = messageSender,
            Type = EventType.MessageReceived
        });
    }
}