using Limp.Client.ClientOnlyModels;
using LimpShared.Models.Message;
using Microsoft.AspNetCore.SignalR.Client;

namespace Limp.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.BinaryFileTransmission;

public interface IFileTransmissionManager
{
    Task SendPackage(ClientMessage message, Func<Task<HubConnection>> getHubConnection);
    Task<bool> StoreDataPackage(Message packageMessage);
    void HandlePackageRegisteredByHub(Guid fileId, int packageIndex);
}