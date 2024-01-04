using Ethachat.Client.ClientOnlyModels;
using EthachatShared.Models.Message;
using EthachatShared.Models.Message.DataTransfer;
using Microsoft.AspNetCore.SignalR.Client;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.BinaryFileTransmission;

public interface IFileTransmissionManager
{
    Task SendDataPackage(Guid fileId, Package package, ClientMessage messageToSend,
        Func<Task<HubConnection>> getHubConnection);
    Task<bool> StoreDataPackage(Message packageMessage);
    void HandlePackageRegisteredByHub(Guid fileId, int packageIndex);
}