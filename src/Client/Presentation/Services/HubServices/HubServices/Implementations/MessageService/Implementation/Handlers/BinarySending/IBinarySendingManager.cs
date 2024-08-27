using EthachatShared.Models.Message.DataTransfer;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.BinarySending;

public interface IBinarySendingManager
{
    IAsyncEnumerable<Package> GetChunksToSendAsync(Package data);
}