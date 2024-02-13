using EthachatShared.Models.Message;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.BinaryReceiving;

public interface IBinaryReceivingManager
{
    Task<(bool, Guid)> StoreAsync(Message message);
}