using Ethachat.Client.ClientOnlyModels;
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
    public async Task HandleAsync(Package clientMessage)
    {
        callbackExecutor.ExecuteSubscriptionsByName(clientMessage.Sender,
            "OnBinaryTransmitting");

        (bool isTransmissionCompleted, Guid fileId) progressStatus =
            await binaryReceivingManager.StoreAsync(clientMessage);

        if (progressStatus.isTransmissionCompleted)
        {
            await messageService.SendMessage(new ClientMessage
            {
                Target = clientMessage.Sender,
                Sender = clientMessage.Target,
                Id = progressStatus.fileId,
                Type = MessageType.DataTransferConfirmation
            });
        }
    }
}