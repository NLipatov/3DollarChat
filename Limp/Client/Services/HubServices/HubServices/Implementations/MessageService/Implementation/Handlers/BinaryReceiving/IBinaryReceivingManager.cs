using Ethachat.Client.ClientOnlyModels;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.BinaryReceiving;

public interface IBinaryReceivingManager
{
    Task<(bool, Guid)> StoreAsync(ClientMessage message);
}