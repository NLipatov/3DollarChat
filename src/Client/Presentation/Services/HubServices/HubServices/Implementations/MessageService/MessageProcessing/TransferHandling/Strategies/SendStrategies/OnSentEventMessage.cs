using Client.Transfer.Domain.Entities.Events;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Handlers;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.
    TransferHandling.Strategies.SendStrategies;

public class OnSentEventMessage(IMessageService messageService) : IStrategyHandler<EventMessage>
{
    public async Task HandleAsync(EventMessage message) => await messageService.TransferAsync(message);
}