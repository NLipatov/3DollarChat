using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.
    BinarySending;
using EthachatShared.Models.Message.DataTransfer;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.
    TransferHandling.Strategies.SendStrategies;

public class PackageSendStrategy(IMessageService messageService, IBinarySendingManager binarySendingManager)
    : ITransferHandler<Package>
{
    public async Task HandleAsync(Package message)
    {
        await foreach (var dataPartMessage in binarySendingManager.GetChunksToSendAsync(message))
            await messageService.TransferAsync(dataPartMessage);
    }
}