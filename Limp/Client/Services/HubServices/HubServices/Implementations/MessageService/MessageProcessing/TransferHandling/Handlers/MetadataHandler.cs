using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.
    BinaryReceiving;
using EthachatShared.Models.Message;

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