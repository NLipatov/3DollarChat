using Ethachat.Client.ClientOnlyModels;
using EthachatShared.Models.Message;
using Microsoft.AspNetCore.SignalR.Client;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.BinarySending;

public interface IBinarySendingManager
{
    Task SendMetadata(Message message, Func<Task<HubConnection>> getHubConnection);
    Task SendFile(ClientMessage message, Func<Task<HubConnection>> getHubConnection);
    void HandlePackageRegisteredByHub(Guid fileId, int packageIndex);
}