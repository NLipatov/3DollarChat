using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.
    BinarySending;
using EthachatShared.Models.Message.DataTransfer;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.
    TransferHandling.Strategies.SendStrategies;

public class SendFileStrategy(IMessageService messageService, IBinarySendingManager binarySendingManager)
    : ITransferHandler<Package>
{
    public async Task HandleAsync(Package eventMessage)
    {
        await foreach (var dataPartMessage in binarySendingManager.GetChunksToSendAsync(eventMessage))
            await messageService.TransferAsync(dataPartMessage);
    }
}