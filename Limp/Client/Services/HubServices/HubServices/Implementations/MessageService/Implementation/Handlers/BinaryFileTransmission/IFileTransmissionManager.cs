using Ethachat.Client.ClientOnlyModels;
using EthachatShared.Models.Message;
using EthachatShared.Models.Message.DataTransfer;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.SignalR.Client;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.BinaryFileTransmission;

public interface IFileTransmissionManager
{
    Task SendMetadata(Message message, Func<Task<HubConnection>> getHubConnection);
    Task SendFile(ClientMessage message, Func<Task<HubConnection>> getHubConnection);
    Task SendDataPackage(Guid fileId, Package package, ClientMessage messageToSend,
        Func<Task<HubConnection>> getHubConnection);
    Task<bool> StoreDataPackage(Message packageMessage);
    void StoreMetadata(Message metadataMessage);
    void HandlePackageRegisteredByHub(Guid fileId, int packageIndex);
}