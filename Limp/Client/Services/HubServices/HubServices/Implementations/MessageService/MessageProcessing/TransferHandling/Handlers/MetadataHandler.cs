using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.
    BinaryReceiving;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.
    TransferHandling.Handlers;

public class MetadataHandler(
    ICallbackExecutor callbackExecutor,
    IBinaryReceivingManager binaryReceivingManager,
    IMessageService messageService) : ITransferHandler<ClientMessage>
{
    public async Task HandleAsync(ClientMessage clientMessage)
    {
        callbackExecutor.ExecuteSubscriptionsByName(clientMessage.Sender,
            "OnBinaryTransmitting");

        (bool isTransmissionCompleted, Guid fileId) progressStatus =
            await binaryReceivingManager.StoreAsync(clientMessage);

        if (progressStatus.isTransmissionCompleted)
        {
            await messageService.NotifyAboutSuccessfullDataTransfer(progressStatus.fileId,
                clientMessage.Sender ??
                throw new ArgumentException($"Invalid {clientMessage.Sender}"));
        }
    }
}