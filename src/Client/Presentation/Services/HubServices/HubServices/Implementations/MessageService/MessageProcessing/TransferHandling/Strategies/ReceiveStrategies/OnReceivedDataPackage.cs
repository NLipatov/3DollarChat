using Client.Transfer.Domain.Entities.Events;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.BinaryReceiving;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Handlers;
using EthachatShared.Models.Message.DataTransfer;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Strategies.ReceiveStrategies;

public class OnReceivedDataPackage(
    ICallbackExecutor callbackExecutor,
    IBinaryReceivingManager binaryReceivingManager,
    IMessageService messageService) : IStrategyHandler<Package>
{
    public async Task HandleAsync(Package package)
    {
        callbackExecutor.ExecuteSubscriptionsByName(package.Sender,
            "OnBinaryTransmitting");

        (bool isTransmissionCompleted, Guid fileId) progressStatus =
            await binaryReceivingManager.StoreAsync(package);

        if (progressStatus.isTransmissionCompleted)
        {
            await messageService.TransferAsync(new EventMessage
            {
                Target = package.Sender,
                Sender = package.Target,
                Id = progressStatus.fileId,
                Type = EventType.DataTransferConfirmation
            });
        }
    }
}