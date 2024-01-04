using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.Services.DataTransmission.PackageForming.Models;
using Ethachat.Client.Services.DataTransmission.PackageForming.Models.TransmittedBinaryFileModels;
using Microsoft.AspNetCore.Components.Forms;

namespace Ethachat.Client.Services.DataTransmission.PackageForming;

public interface IPackageMultiplexerService
{
    string Combine(List<ClientPackage> packages, string partnerUsername);
    Task<List<string>> SplitAsync(IBrowserFile file);
    Task<ChunkableBinary> Split(IBrowserFile file);
}