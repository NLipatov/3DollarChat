using Client.Transfer.Domain.TransferedEntities.Events;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Handlers;
using Ethachat.Client.Services.InboxService;
using EthachatShared.Models.Message;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Strategies.ReceiveStrategies;

public class OnReceivedResendRequest(IMessageBox messageBox, IMessageService messageService) : IStrategyHandler<EventMessage>
{
    public async Task HandleAsync(EventMessage textMessage)
    {
        var message = messageBox.Messages.FirstOrDefault(x => x.Id == textMessage.Id);
        if (message?.Type is MessageType.TextMessage)
        {
            await messageService.TransferAsync(message);
        }
    }
}