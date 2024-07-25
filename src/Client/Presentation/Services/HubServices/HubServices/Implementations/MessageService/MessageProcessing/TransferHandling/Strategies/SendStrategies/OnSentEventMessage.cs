using Ethachat.Client.ClientOnlyModels.Events;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.
    TransferHandling.Strategies.SendStrategies;

public class OnSentEventMessage(IMessageService messageService) : ITransferHandler<EventMessage>
{
    public async Task HandleAsync(EventMessage message) => await messageService.TransferAsync(message);
}