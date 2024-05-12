using Ethachat.Client.ClientOnlyModels;
using EthachatShared.Models.Message;
using Microsoft.AspNetCore.SignalR.Client;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.BinarySending;

public interface IBinarySendingManager
{
    Task SendMetadata(Message message, Func<Task<HubConnection>> getHubConnection);
    IAsyncEnumerable<ClientMessage> SendFile(ClientMessage message);
    void HandlePackageRegisteredByHub(Guid fileId, int packageIndex);
}