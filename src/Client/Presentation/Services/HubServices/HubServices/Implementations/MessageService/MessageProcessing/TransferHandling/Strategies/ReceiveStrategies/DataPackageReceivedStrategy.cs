using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.ClientOnlyModels.Events;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.BinaryReceiving;
using EthachatShared.Models.Message;
using EthachatShared.Models.Message.DataTransfer;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Strategies.ReceiveStrategies;

public class DataPackageReceivedStrategy(
    ICallbackExecutor callbackExecutor,
    IBinaryReceivingManager binaryReceivingManager,
    IMessageService messageService) : ITransferHandler<Package>
{
    public async Task HandleAsync(Package eventMessage)
    {
        callbackExecutor.ExecuteSubscriptionsByName(eventMessage.Sender,
            "OnBinaryTransmitting");

        (bool isTransmissionCompleted, Guid fileId) progressStatus =
            await binaryReceivingManager.StoreAsync(eventMessage);

        if (progressStatus.isTransmissionCompleted)
        {
            await messageService.SendMessage(new EventMessage
            {
                Target = eventMessage.Sender,
                Sender = eventMessage.Target,
                Id = progressStatus.fileId,
                Type = EventType.DataTransferConfirmation
            });
        }
    }
}