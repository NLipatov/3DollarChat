using EthachatShared.Models.Message.DataTransfer;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.BinaryReceiving;

public interface IBinaryReceivingManager
{
    Task<(bool, Guid)> StoreAsync(Package message);
}