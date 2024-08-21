using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.
    BinarySending;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Handlers;
using EthachatShared.Models.Message.DataTransfer;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.
    TransferHandling.Strategies.SendStrategies;

public class OnSentPackage(IMessageService messageService, IBinarySendingManager binarySendingManager)
    : IStrategyHandler<Package>
{
    public async Task HandleAsync(Package message)
    {
        await foreach (var dataPartMessage in binarySendingManager.GetChunksToSendAsync(message))
            await messageService.TransferAsync(dataPartMessage);
    }
}