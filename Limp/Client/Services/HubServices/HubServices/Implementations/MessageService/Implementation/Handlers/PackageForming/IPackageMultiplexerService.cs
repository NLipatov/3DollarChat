using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.PackageForming.Models.TransmittedBinaryFileModels;
using Microsoft.AspNetCore.Components.Forms;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.PackageForming;

public interface IPackageMultiplexerService
{
    Task<ChunkableBinary> SplitAsync(IBrowserFile file);
    Task CombineAsync(MemoryStream memoryStream, IEnumerable<ClientPackage> packages);
}