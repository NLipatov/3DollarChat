using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.Services.DataTransmission.PackageForming.Models.TransmittedBinaryFileModels;
using Microsoft.AspNetCore.Components.Forms;

namespace Ethachat.Client.Services.DataTransmission.PackageForming;

public interface IPackageMultiplexerService
{
    Task<ChunkableBinary> Split(IBrowserFile file);
    Task CombineAsync(MemoryStream memoryStream, IEnumerable<ClientPackage> packages);
}