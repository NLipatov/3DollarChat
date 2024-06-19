using Ethachat.Client.ClientOnlyModels;
using EthachatShared.Models.Message;
using Microsoft.AspNetCore.SignalR.Client;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.BinarySending;

public interface IBinarySendingManager
{
    IAsyncEnumerable<ClientMessage> GetChunksToSendAsync(ClientMessage message);
    void HandlePackageRegisteredByHub(Guid fileId, int packageIndex);
}