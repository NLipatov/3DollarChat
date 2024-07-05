using Ethachat.Client.ClientOnlyModels.Events;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.BinaryReceiving;
using EthachatShared.Models.Message.DataTransfer;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Strategies.ReceiveStrategies;

public class DataPackageReceivedStrategy(
    ICallbackExecutor callbackExecutor,
    IBinaryReceivingManager binaryReceivingManager,
    IMessageService messageService) : ITransferHandler<Package>
{
    public async Task HandleAsync(Package package)
    {
        callbackExecutor.ExecuteSubscriptionsByName(package.Sender,
            "OnBinaryTransmitting");

        (bool isTransmissionCompleted, Guid fileId) progressStatus =
            await binaryReceivingManager.StoreAsync(package);

        if (progressStatus.isTransmissionCompleted)
        {
            await messageService.SendMessage(new EventMessage
            {
                Target = package.Sender,
                Sender = package.Target,
                Id = progressStatus.fileId,
                Type = EventType.DataTransferConfirmation
            });
        }
    }
}