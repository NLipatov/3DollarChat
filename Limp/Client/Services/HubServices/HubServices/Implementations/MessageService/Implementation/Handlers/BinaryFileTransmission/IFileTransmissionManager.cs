using Ethachat.Client.ClientOnlyModels;
using EthachatShared.Models.Message;
using Microsoft.AspNetCore.SignalR.Client;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.BinaryFileTransmission;

public interface IFileTransmissionManager
{
    Task SendMetadata(Message message, Func<Task<HubConnection>> getHubConnection);
    Task SendFile(ClientMessage message, Func<Task<HubConnection>> getHubConnection);
    void HandlePackageRegisteredByHub(Guid fileId, int packageIndex);
}